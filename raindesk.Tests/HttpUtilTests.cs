using raindesk.Util;

namespace raindesk.Tests
{
    [TestClass]
    public class HttpUtilTests
    {
        [TestMethod]
        public void Get_PlainHttpUrl_ReturnsEmptyWithoutRequest()
        {
            string result = HttpUtil.Get("http://insecure.example/update");
            Assert.AreEqual("", result, "明文 HTTP 端点必须被拒绝，避免更新元数据被篡改");
        }

        [TestMethod]
        public void Get_MalformedUrl_ReturnsEmpty()
        {
            string result = HttpUtil.Get("not a url");
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void Get_EmptyUrl_ReturnsEmpty()
        {
            Assert.AreEqual("", HttpUtil.Get(""));
            Assert.AreEqual("", HttpUtil.Get(null));
        }

        [TestMethod]
        public void Post_PlainHttpUrl_ReturnsEmptyWithoutRequest()
        {
            string result = HttpUtil.Post("http://insecure.example/api", "{}");
            Assert.AreEqual("", result, "明文 HTTP 端点必须被拒绝");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GetAsync_PlainHttpUrl_ReturnsEmptyWithoutRequest()
        {
            string result = await HttpUtil.GetAsync("http://insecure.example/update");
            Assert.AreEqual("", result, "明文 HTTP 端点必须被拒绝");
        }
    }
}
