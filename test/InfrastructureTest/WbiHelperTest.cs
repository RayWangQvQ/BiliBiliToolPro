using System.Diagnostics;
using System.Text.RegularExpressions;

namespace InfrastructureTest
{
    public class WbiHelperTest
    {
        [Fact]
        public void Replace_Test()
        {
            string input = "这是一段包含特殊字符!@#$%^&*(')的字符串";
            string pattern = "[!'()*]";
            string replacement = "";

            string output = Regex.Replace(input, pattern, replacement);
            Debug.WriteLine(output);

            Assert.Equal(output, "这是一段包含特殊字符@#$%^&的字符串");
        }
    }
}
