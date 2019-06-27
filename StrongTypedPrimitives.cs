/*

Strong typed OrderId to prevent primitive obsession. 
This file contains a struct definition, as an example 
how it is possible to prevent primitive obsession, for 
keys (i.c. GUID's)

Why a struct:
- no heap allocation, when this is defined as a class, a previously
  primitive (int, char, bool, guid) would be allocated on the heap.
- fast
- solving primitive obsession for classes is simpler, as serialization
  and deserialization of classes is wel defined
  
Remarks:
- Should you always define types to avoid primitive obsession?
- When is the extra overhead (code complexity) worth the effort?
- When using this pattern to avoid parameter swapping, introducing 
  a POCO class to contain the parameters, is also an option
  
More info:
https://refactoring.guru/smells/primitive-obsession
https://docs.microsoft.com/en-us/dotnet/api/system.guid?view=netcore-2.2

*/
using System;
using Newtonsoft.Json;
using Xunit;

namespace StrongTypedTests
{
    using StrongTyped;
    public class StrongTypedPrimitives
    {
        [Fact]
        public void TypesShouldSerialize()
        {
            var productId = ProductId.New();
            var s = JsonConvert.SerializeObject(productId);
            var newId = JsonConvert.DeserializeObject<ProductId>(s);
            Assert.Equal((object)productId, (object)newId);
        }

        [Fact]
        public void TypesShouldRemainPrimitive()
        {
            var productId = ProductId.New();
            Assert.True( typeof(ProductId).IsValueType);
        }
   }
}

namespace StrongTyped
{
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Strong typed ProductId to prevent primitive obsession. 
    /// </summary>
    [JsonConverter(typeof(ProductIdJsonConverter))]
    [TypeConverter(typeof(ProductIdTypeConverter))]
    public readonly struct ProductId : IComparable<ProductId>, IEquatable<ProductId>
    {
        public Guid Value { get; }

        public ProductId(Guid value)
        {
            Value = value;
        }

        public static ProductId New() => new ProductId(Guid.NewGuid());
        public static ProductId Empty { get; } = new ProductId(Guid.Empty);

        public bool Equals(ProductId other) => Value.Equals(other.Value);
        public int CompareTo(ProductId other) => Value.CompareTo(other.Value);

        [SuppressMessage(
          "Microsoft.Design",
          "IDE0041",
          Justification = "equality check is overloaded")]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ProductId other && Equals(other);
        }

        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(ProductId a, ProductId b) => a.CompareTo(b) == 0;
        public static bool operator !=(ProductId a, ProductId b) => !(a == b);

        #region TypeConvertors
        // These type converters are needed for serialization 
        // and deserialization of the object
        private class ProductIdJsonConverter : JsonConverter
        {
            public override void WriteJson(
              JsonWriter writer,
              object value,
              JsonSerializer serializer)
            {
                var id = (ProductId)value;
                serializer.Serialize(writer, id.Value);
            }

            public override object ReadJson(
              JsonReader reader,
              Type objectType,
              object existingValue,
              JsonSerializer serializer)
            {
                var guid = serializer.Deserialize<Guid>(reader);
                return new ProductId(guid);
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ProductId);
            }
        }

        private class ProductIdTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(
              ITypeDescriptorContext context,
              Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(
              ITypeDescriptorContext context,
              CultureInfo culture,
              object value)
            {
                var stringValue = value as string;
                if (!string.IsNullOrEmpty(stringValue)
                    && Guid.TryParse(stringValue, out var guid))
                {
                    return new ProductId(guid);
                }
                return base.ConvertFrom(context, culture, value);
            }
        }
        #endregion
    }
}
