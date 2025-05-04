using System.Diagnostics;
using System.Text.RegularExpressions;

namespace InfrastructureTest
{
    public class WbiHelperTest
    {
        [Fact]
        public void Replace_Test()
        {
            string input = "����һ�ΰ��������ַ�!@#$%^&*(')���ַ���";
            string pattern = "[!'()*]";
            string replacement = "";

            string output = Regex.Replace(input, pattern, replacement);
            Debug.WriteLine(output);

            Assert.Equal("����һ�ΰ��������ַ�@#$%^&���ַ���", output);
        }
    }
}
