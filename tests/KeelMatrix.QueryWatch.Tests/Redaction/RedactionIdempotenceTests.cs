using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class RedactionIdempotenceTests {
        [Fact]
        public void Each_Redactor_Is_Idempotent_On_Common_Sample() {
            // A single string that contains many shapes; any unknown shapes will stay as-is.
            string sample = @"
                X-Api-Key: SECRET
                Authorization: Bearer aaaBBBBBBBBB.cccDDDDDDDDD.eeeEEEEEEEEE
                Cookie: a=1; b=2
                Set-Cookie: session=abc; Secure
                accountkey=abc
                Password=TopSecret; Pwd = s3cr3t
                contact: admin@example.com
                token=TTT&access_token=AAA&code=CCC&id_token=DDD&auth=EEE
                " + new string('a', 32) + @"
                123e4567-e89b-12d3-a456-426614174000
                0123456789abcDEF
                AIza" + new string('A', 35) + @"
                2001:0db8:85a3:0000:0000:8a2e:0370:7334
                192.168.1.10
                +1 (555) 012-3456
                ";

            IQueryTextRedactor[] redactors = [
                new ApiKeyRedactor(),
                new AuthorizationRedactor(),
                new CookieRedactor(),
                new AzureKeyLikeRedactor(),
                new ConnectionStringPasswordRedactor(),
                new EmailRedactor(),
                new UrlQueryTokenRedactor(),
                new LongHexTokenRedactor(),
                new GuidRedactor(),
                new GuidLikeHexRedactor(),
                new GoogleApiKeyRedactor(),
                new IpAddressRedactor(),
                new PhoneRedactor(),
                new UuidNoDashRedactor(),
                new WhitespaceNormalizerRedactor(),
            ];

            foreach (IQueryTextRedactor r in redactors) {
                string once = r.Redact(sample);
                string twice = r.Redact(once);
                _ = twice.Should().Be(once, $"{r.GetType().Name} should be idempotent");
            }
        }
    }
}
