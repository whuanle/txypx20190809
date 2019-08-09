using CosTest;
using System;
using System.Threading.Tasks;

namespace CosTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 构建一个 CoxXmlServer 对象
            var cosClient = new CosBuilder()
                .SetAccount("1252744", "ap-guangzhou")
                .SetCosXmlServer()
                .SetSecret("AKIDEZohWs462rVxLIpG", "Sn1iFi182jMAOE5rSwG")
                .Builder();
            // 创建Cos连接客户端
            ICosClient client = new CosClient(cosClient, "1257544");
            // 创建一个存储桶
            var result = await client.CreateBucket("fsdgerer");
            Console.WriteLine("处理结果：" + result.Message);
            // 查询存储桶列表
            var c = await client.SelectBucket();
            Console.WriteLine(c.Message + c.Data);

            Console.ReadKey();
        }
    }
}
