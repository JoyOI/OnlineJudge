using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using Xunit;
using JoyOI.OnlineJudge.WebApi.Controllers.Api;

namespace JoyOI.OnlineJudge.WebApi.Tests
{
    public class CookieExpireRegexTests
    {
        [Fact]
        public void Cookie_expire_regex_tests()
        {
            var regex = (Regex)typeof(UserController).GetField("CookieExpireRegex", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var match = regex.Match(".AspNetCore.Identity.Application=CfDJ8Fy8JmgAuuxGpRboGRtpB7n5Uxq-TtjndciY_P7OYNqhrGW-B5PXki3kVfCUgWMFMkH3MKdC0WSXVtn2RO1AeNJGQ9k4aKPyjxQlJvCxzJel3WAErHXzBYjuWWCfZdZyDJrpdBkKy8Nb3MHLCmPdA1P0duBHHb7fwDT55y78YpB1BgBEybCNCUcnhnd3tBxgad1mGbk0w1zMNceNJZFRPb3MeuqlbdSAjMg4RtcO9rmZohtVMVCtGoG9JXbSjL4IjcucjeQw93-TzrNqV3laL6bdSAV8msPZuYRpVQgDP0jsSCRDt18rZ1alue_Ul-hywcjA8Di1wS8iYUIDXAfgvnWLDiw_2bbpx3YDP2UEjpLAmN3AcBvh4w0drmzg1iCn33LoOsOCVcBf3p-yVRfofMIhGMLutZ2y7RQxc2b26K1wdWGx2hsWr901LzmPXYfQIiuV-EUDwfgBu_OwRob_49dIZPesqQPJlsZwTfs66_5b6g7Dost06sn2rNcNflRLthwY-DeA0Y91r4_TDaFQ3jga0ZdpZm8t69Nim8d-GUm7h1ggPz7bclxzHs7hUqVGdK-Jwc2XEz9Kin5YCWOqJfEY7TBxUo1Bcv0O1V1OhhYt; expires=Tue, 22 Aug 2017 13:12:53 GMT; path=/; samesite=lax; httponly");

            Assert.Equal("Tue, 22 Aug 2017 13:12:53 GMT", match.Value);
            Assert.True(DateTimeOffset.TryParse(match.Value, out var time));
        }
    }
}
