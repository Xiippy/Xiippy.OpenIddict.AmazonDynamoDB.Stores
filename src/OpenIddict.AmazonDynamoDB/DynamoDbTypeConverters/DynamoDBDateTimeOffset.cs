/*******************************************************************************************
Copyright © 2019 Xiippy.ai. All rights reserved. Australian patents awarded. PCT patent pending.

NOTES:

- No payment gateway SDK function is consumed directly. Interfaces are defined out of such interactions and then the interface is implemented for payment gateways. Design the interface with the most common members and data structures between different gateways. 
A proper factory or provider must instantiate an instance of the interface that is interacted with.
- Any major change made to SDKs should begin with the c# SDK with the mindset to keep the high-level syntax, structures and class names the same to minimise porting efforts to other languages. Do not use language specific features that don't exist in other languages. We are not in the business of doing the same thing from scratch multiple times in different forms.
- Pascal Case for naming conventions should be used for all languages
- No secret or passwords or keys must exist in the code when checked in

*******************************************************************************************/

using System.Globalization;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace OpenIddict.AmazonDynamoDB.DynamoDbTypeConverters
{


  public class DynamoDBDateTimeOffset : IPropertyConverter
  {
    public static string FormatIso8601(DateTimeOffset dto)
    {
      var format = dto.Offset == TimeSpan.Zero
          ? "yyyy-MM-ddTHH:mm:ss.fffZ"
          : "yyyy-MM-ddTHH:mm:ss.fffzzz";

      return dto.ToString(format, CultureInfo.InvariantCulture);
    }
    public static DateTimeOffset ParseIso8601(string iso8601String)
    {
      DateTimeOffset result;
      return DateTimeOffset.TryParseExact(
          iso8601String,
          new string[] { "yyyy-MM-dd'T'HH:mm:ss.FFFK" },
          CultureInfo.InvariantCulture,
          DateTimeStyles.None, out result) ? result : DateTimeOffset.MinValue;
    }
    public DynamoDBEntry ToEntry(object value)
    {
      var dtoffset = (DateTimeOffset)value;

      string data = null;
      if (dtoffset != null)
        data = FormatIso8601(dtoffset);


      DynamoDBEntry entry = new Primitive
      {
        Value = data
      };
      return entry;
    }

    public object FromEntry(DynamoDBEntry entry)
    {
      var primitive = entry as Primitive;
      if (primitive == null || !(primitive.Value is string) || string.IsNullOrEmpty((string)primitive.Value))
        return DateTimeOffset.MinValue;

      var data = (string)primitive.Value;

      var dtoffset = ParseIso8601(data);
      return dtoffset;
    }
  }
}
