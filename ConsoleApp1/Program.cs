using CosTest;
using System;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cosClient = new CosBuilder()
                .SetAccount("1252707544", "	ap-guangzhou")
                .SetCosXmlServer()
                .SetSecret("AKIDEZohU6AmkeNTVPmedw65Ws462rVxLIpG", "Sn1iFi182jMARcheQ1gYIsGSROE5rSwG")
                .Builder();
            ICosClient client = new CosClient(cosClient, "1252707544");
            var result = await client.CreateBucket("fsdgerer");
            Console.WriteLine("处理结果：" + result.Message);
            var c = await client.SelectBucket();
            Console.WriteLine(c.Message + c.Data);


            IBucketClient bucketclient = new BucketClient(cosClient, "work-1252707544", "1252707544");
            var re = await bucketclient.SelectObjectList();
            Console.WriteLine(re.Data);
            Console.ReadKey();
        }
    }
}
