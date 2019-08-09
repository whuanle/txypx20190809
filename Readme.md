## 如何优雅地使用腾讯云COS-.NET篇

### 前提

**创建子账号**

打开 https://console.cloud.tencent.com/cam

创建子用户，设置子账号策略为 `AdministratorAccess` ，或者参考https://cloud.tencent.com/document/product/436/11714 ，添加访问 COS 的权限 记录子用户的 `账号ID`。

切换子用户登录。

**添加 appid 密钥**

打开 https://console.cloud.tencent.com/cam/capi

新建 API 密钥，记录下 appid，记录 `SecretId` 和 `SecretKey`。

**记录 Region**

打开 https://cloud.tencent.com/document/product/436/6224

可以查询可用区/地域的 region。

本教程使用 C# 开发。



## 一，SDK 和使用

腾讯云官网提供了 .NET 版本的 对象存储(COS) SDK，并提供使用教程，教程链接：

https://cloud.tencent.com/document/product/436/32819

Nuget 搜索 `Tencent.QCloud.Cos.Sdk` 安装即可。

using 需引入

```c#
using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using COSXML.Model.Bucket;
using COSXML.CosException;
using COSXML.Utils;
using COSXML.Model.Service;
using COSXML.Transfer;
using COSXML.Model;
```

根据官方的教程，很容易编写自己的软件：

<kbd>Ctrl</kbd> + <kbd>C</kbd>  ，然后 <kbd>Ctrl</kbd> + <kbd>V</kbd>

拷贝完毕，大概是这样的

```c#
using System;
using System.Collections.Generic;
using System.Text;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using COSXML.Model.Bucket;
using COSXML.CosException;
using COSXML.Utils;
using COSXML.Model.Service;
using COSXML.Transfer;
using COSXML.Model;

namespace CosTest
{
    public class CosClient
    {
        CosXmlServer cosXml;
        private readonly string _appid;
        private readonly string _region;
        public CosClient(string appid, string region)
        {
            _appid = appid;
            _region = region;

            //初始化 CosXmlConfig 
            //string appid = "100011070645";
            //string region = "ap-guangzhou"; 
            CosXmlConfig config = new CosXmlConfig.Builder()
                .SetConnectionTimeoutMs(60000)
                .SetReadWriteTimeoutMs(40000)
                .IsHttps(true)
                .SetAppid(appid)
                .SetRegion(region)
                .SetDebugLog(true)
                .Build();

            QCloudCredentialProvider cosCredentialProvider = null;

            string secretId = "AKID62jALHsVmpfHentPs9E6lBMJ2XnnsTzH"; //"云 API 密钥 SecretId";
            string secretKey = "CC0c1DAtNdfS0IPIvISRFtIUSCUYTAgy"; //"云 API 密钥 SecretKey";
            long durationSecond = 600;  //secretKey 有效时长,单位为 秒
            cosCredentialProvider = new DefaultQCloudCredentialProvider(secretId, secretKey, durationSecond);

            //初始化 CosXmlServer
            cosXml = new CosXmlServer(config, cosCredentialProvider);
        }
        public bool CreateBucket(string buketName)
        {
            try
            {
                string bucket = buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                PutBucketRequest request = new PutBucketRequest(buketName);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //执行请求
                PutBucketResult result = cosXml.PutBucket(request);
                //请求成功
                Console.WriteLine(result.GetResultInfo());
                return true;
            }
            catch (COSXML.CosException.CosClientException clientEx)
            {
                //请求失败
                Console.WriteLine("CosClientException: " + clientEx.Message);
                return false;
            }
            catch (COSXML.CosException.CosServerException serverEx)
            {
                //请求失败
                Console.WriteLine("CosServerException: " + serverEx.GetInfo());
                return false;
            }
        }
        public bool SelectBucket()
        {
            try
            {
                GetServiceRequest request = new GetServiceRequest();
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //执行请求
                GetServiceResult result = cosXml.GetService(request);
                //请求成功
                Console.WriteLine(result.GetResultInfo());
                return true;
            }
            catch (COSXML.CosException.CosClientException clientEx)
            {
                //请求失败
                Console.WriteLine("CosClientException: " + clientEx.Message);
                return false;
            }
            catch (COSXML.CosException.CosServerException serverEx)
            {
                //请求失败
                Console.WriteLine("CosServerException: " + serverEx.GetInfo());
                return false;
            }
        }

        public bool Upfile(string buketName, string key, string srcPath)
        {
            try
            {
                string bucket = buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                PutObjectRequest request = new PutObjectRequest(bucket, key, srcPath);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //设置进度回调
                request.SetCosProgressCallback(delegate (long completed, long total)
                {
                    Console.WriteLine(String.Format("progress = {0:##.##}%", completed * 100.0 / total));
                });
                //执行请求
                PutObjectResult result = cosXml.PutObject(request);
                //请求成功
                Console.WriteLine(result.GetResultInfo());
                return true;
            }
            catch (COSXML.CosException.CosClientException clientEx)
            {
                //请求失败
                Console.WriteLine("CosClientException: " + clientEx.Message);
                return false;
            }
            catch (COSXML.CosException.CosServerException serverEx)
            {
                //请求失败
                Console.WriteLine("CosServerException: " + serverEx.GetInfo());
                return false;
            }
        }
        public void UpBigFile(string buketName, string key, string srcPath)
        {
            string bucket = buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID

            TransferManager transferManager = new TransferManager(cosXml, new TransferConfig());
            COSXMLUploadTask uploadTask = new COSXMLUploadTask(bucket, null, key);
            uploadTask.SetSrcPath(srcPath);
            uploadTask.progressCallback = delegate (long completed, long total)
            {
                Console.WriteLine(String.Format("progress = {0:##.##}%", completed * 100.0 / total));
            };
            uploadTask.successCallback = delegate (CosResult cosResult)
            {
                COSXML.Transfer.COSXMLUploadTask.UploadTaskResult result = cosResult as COSXML.Transfer.COSXMLUploadTask.UploadTaskResult;
                Console.WriteLine(result.GetResultInfo());
            };
            uploadTask.failCallback = delegate (CosClientException clientEx, CosServerException serverEx)
            {
                if (clientEx != null)
                {
                    Console.WriteLine("CosClientException: " + clientEx.Message);
                }
                if (serverEx != null)
                {
                    Console.WriteLine("CosServerException: " + serverEx.GetInfo());
                }
            };
            transferManager.Upload(uploadTask);
        }
        public class ResponseModel
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public dynamic Data { get; set; }
        }
        public ResponseModel SelectObjectList(string buketName)
        {
            try
            {
                string bucket = buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                GetBucketRequest request = new GetBucketRequest(bucket);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //执行请求
                GetBucketResult result = cosXml.GetBucket(request);
                //请求成功
                Console.WriteLine(result.GetResultInfo());
                return new ResponseModel { Code = 200, Data = result.GetResultInfo() };
            }
            catch (COSXML.CosException.CosClientException clientEx)
            {
                //请求失败
                Console.WriteLine("CosClientException: " + clientEx.Message);
                return new ResponseModel { Code = 200, Data = clientEx.Message };
            }
            catch (COSXML.CosException.CosServerException serverEx)
            {
                //请求失败
                Console.WriteLine("CosServerException: " + serverEx.GetInfo());
                return new ResponseModel { Code = 200, Data = serverEx.Message };
            }
        }
        public bool DownObject(string buketName, string key, string localDir, string localFileName)
        {
            try
            {
                string bucket = buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                GetObjectRequest request = new GetObjectRequest(bucket, key, localDir, localFileName);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //设置进度回调
                request.SetCosProgressCallback(delegate (long completed, long total)
                {
                    Console.WriteLine(String.Format("progress = {0:##.##}%", completed * 100.0 / total));
                });
                //执行请求
                GetObjectResult result = cosXml.GetObject(request);
                //请求成功
                Console.WriteLine(result.GetResultInfo());
                return true;
            }
            catch (COSXML.CosException.CosClientException clientEx)
            {
                //请求失败
                Console.WriteLine("CosClientException: " + clientEx.Message);
                return false;
            }
            catch (COSXML.CosException.CosServerException serverEx)
            {
                //请求失败
                Console.WriteLine("CosServerException: " + serverEx.GetInfo());
                return false;
            }
        }
        public bool DeleteObject(string buketName)
        {
            try
            {
                string bucket = buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                string key = "exampleobject"; //对象在存储桶中的位置，即称对象键.
                DeleteObjectRequest request = new DeleteObjectRequest(bucket, key);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //执行请求
                DeleteObjectResult result = cosXml.DeleteObject(request);
                //请求成功
                Console.WriteLine(result.GetResultInfo());
                return true;
            }
            catch (COSXML.CosException.CosClientException clientEx)
            {
                //请求失败
                Console.WriteLine("CosClientException: " + clientEx.Message);
                return false;
            }
            catch (COSXML.CosException.CosServerException serverEx)
            {
                //请求失败
                Console.WriteLine("CosServerException: " + serverEx.GetInfo());
                return false;
            }
        }
    }
}


```

概览：

![1565341618](https://obs1.whuanle.cn/2019-08/09/1565341618.png)



但是大神说，这样不好，程序要高内聚、低耦合，依赖与抽象而不依赖于具体实现。

那怎么办？只能修改一下。



## 二，优化

#### 1，对象构建器

对象存储的 SDK 中，有三个重要的对象：

- `CosXmlConfig` 提供配置 SDK 接口。
- `QCloudCredentialProvider` 提供设置密钥信息接口。
- `CosXmlServer` 提供各种 COS API 服务接口。

但是，初始化和配置对象，过于麻烦，那么我们做一个对象构建器，实现函数式编程和链式语法。



```c#
    /// <summary>
    /// 生成Cos客户端工具类
    /// </summary>
    public class CosBuilder
    {
        private CosXmlServer cosXml;
        private string _appid;
        private string _region;
        private CosXmlConfig cosXmlConfig;
        private QCloudCredentialProvider cosCredentialProvider;
        public CosBuilder()
        {

        }


        public CosBuilder SetAccount(string appid, string region)
        {
            _appid = appid;
            _region = region;
            return this;
        }
        public CosBuilder SetCosXmlServer(int ConnectionTimeoutMs = 60000, int ReadWriteTimeoutMs = 40000, bool IsHttps = true, bool SetDebugLog = true)
        {
            cosXmlConfig = new CosXmlConfig.Builder()
                .SetConnectionTimeoutMs(ConnectionTimeoutMs)
                .SetReadWriteTimeoutMs(ReadWriteTimeoutMs)
                .IsHttps(true)
                .SetAppid(_appid)
                .SetRegion(_region)
                .SetDebugLog(true)
                .Build();
            return this;
        }
        public CosBuilder SetSecret(string secretId, string secretKey, long durationSecond = 600)
        {

            cosCredentialProvider = new DefaultQCloudCredentialProvider(secretId, secretKey, durationSecond);
            return this;
        }
        public CosXmlServer Builder()
        {
            //初始化 CosXmlServer
            cosXml = new CosXmlServer(cosXmlConfig, cosCredentialProvider);
            return cosXml;
        }
    }
```



#### 2，消息响应对象

为了统一返回消息，创建一个 Response Model 的类。

```c#
    /// <summary>
    /// 消息响应
    /// </summary>
    public class ResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public dynamic Data { get; set; }
    }
```



#### 3，接口

实际上，访问 COS和控制，和存储桶内的操作，是可以分开的。

访问 COS 可以控制对象存储内的所有东西，但是每个存储桶又是一个独立的对象。

为了松耦合，我们拆分两个客户端，一个用来管理连接、存储桶等，一个用来管理存储桶内的操作。

接口如下：

```c#

    public interface ICosClient
    {
        // 创建存储桶
        Task<ResponseModel> CreateBucket(string buketName);

        // 获取存储桶列表
        Task<ResponseModel> SelectBucket(int tokenTome = 600);
    }
    public interface IBucketClient
    {
        // 上传文件
        Task<ResponseModel> UpFile(string key, string srcPath);

        // 分块上传大文件
        Task<ResponseModel> UpBigFile(string key, string srcPath, Action<long, long> progressCallback, Action<CosResult> successCallback);

        // 查询存储桶的文件列表
        Task<ResponseModel> SelectObjectList();

        // 下载文件
        Task<ResponseModel> DownObject(string key, string localDir, string localFileName);

        // 删除文件
        Task<ResponseModel> DeleteObject(string buketName);
    }
```

所有接口功能都使用异步实现。



#### 4，COS 客户端

基架代码：

```c#
    public class CosClient : ICosClient
    {
        CosXmlServer _cosXml;
        private readonly string _appid;
        private readonly string _region;
        public CosClient(CosXmlServer cosXml)
        {
            _cosXml = cosXml;
        }

    }
```

创建存储桶：

```c#

        public async Task<ResponseModel> CreateBucket(string buketName)
        {
            try
            {
                string bucket = buketName + "-" + _appid; 
                PutBucketRequest request = new PutBucketRequest(bucket);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //执行请求
                PutBucketResult result = await Task.FromResult(_cosXml.PutBucket(request));

                return new ResponseModel { Code = 200, Message = result.GetResultInfo() };
            }
            catch (COSXML.CosException.CosClientException clientEx)
            {
                //请求失败
                Console.WriteLine();
                return new ResponseModel { Code = 0, Message = "CosClientException: " + clientEx.Message };
            }
            catch (COSXML.CosException.CosServerException serverEx)
            {
                return new ResponseModel { Code = 200, Message = "CosServerException: " + serverEx.GetInfo() };
            }
        }

   
```



查询存储桶列表

```c#
        public async Task<ResponseModel> SelectBucket(int tokenTome = 600)
        {
            try
            {
                GetServiceRequest request = new GetServiceRequest();
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), tokenTome);
                //执行请求
                GetServiceResult result = await Task.FromResult(_cosXml.GetService(request));
                return new ResponseModel { Code = 200, Message = "Success", Data = result.GetResultInfo() };
            }
            catch (COSXML.CosException.CosClientException clientEx)
            {
                return new ResponseModel { Code = 0, Message = "CosClientException: " + clientEx.Message };
            }
            catch (CosServerException serverEx)
            {
                return new ResponseModel { Code = 0, Message = "CosServerException: " + serverEx.GetInfo() };
            }
        }
```



#### 5，存储桶操作客户端

基架代码如下：

```c#
    /// <summary>
    /// 存储桶客户端
    /// </summary>
    public class BucketClient : IBucketClient
    {
        private readonly CosXmlServer _cosXml;
        private readonly string _buketName;
        private readonly string _appid;
        public BucketClient(CosXmlServer cosXml, string buketName, string appid)
        {
            _cosXml = cosXml;
            _buketName = buketName;
            _appid = appid;
        }

    }
```

上传对象

```c#
        public async Task<ResponseModel> UpFile(string key, string srcPath)
        {
            try
            {
                string bucket = _buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                PutObjectRequest request = new PutObjectRequest(bucket, key, srcPath);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //设置进度回调
                request.SetCosProgressCallback(delegate (long completed, long total)
                {
                    Console.WriteLine(String.Format("progress = {0:##.##}%", completed * 100.0 / total));
                });
                //执行请求
                PutObjectResult result = await Task.FromResult(_cosXml.PutObject(request));

                return new ResponseModel { Code = 200, Message = result.GetResultInfo() };
            }
            catch (CosClientException clientEx)
            {
                return new ResponseModel { Code = 0, Message = "CosClientException: " + clientEx.Message };
            }
            catch (CosServerException serverEx)
            {
                return new ResponseModel { Code = 0, Message = "CosServerException: " + serverEx.GetInfo() };
            }
        }
```

大文件分块上传

```c#
        /// <summary>
        /// 上传大文件、分块上传
        /// </summary>
        /// <param name="key"></param>
        /// <param name="srcPath"></param>
        /// <param name="progressCallback">委托，可用于显示分块信息</param>
        /// <param name="successCallback">委托，当任务成功时回调</param>
        /// <returns></returns>
        public async Task<ResponseModel> UpBigFile(string key, string srcPath, Action<long, long> progressCallback, Action<CosResult> successCallback)
        {
            ResponseModel responseModel = new ResponseModel();
            string bucket = _buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID

            TransferManager transferManager = new TransferManager(_cosXml, new TransferConfig());
            COSXMLUploadTask uploadTask = new COSXMLUploadTask(bucket, null, key);
            uploadTask.SetSrcPath(srcPath);
            uploadTask.progressCallback = delegate (long completed, long total)
            {
                progressCallback(completed, total);
                //Console.WriteLine(String.Format("progress = {0:##.##}%", completed * 100.0 / total));
            };
            uploadTask.successCallback = delegate (CosResult cosResult)
            {
                COSXMLUploadTask.UploadTaskResult result = cosResult as COSXMLUploadTask.UploadTaskResult;
                successCallback(cosResult);
                responseModel.Code = 200;
                responseModel.Message = result.GetResultInfo();
            };
            uploadTask.failCallback = delegate (CosClientException clientEx, CosServerException serverEx)
            {
                if (clientEx != null)
                {
                    responseModel.Code = 0;
                    responseModel.Message = clientEx.Message;
                }
                if (serverEx != null)
                {
                    responseModel.Code = 0;
                    responseModel.Message = "CosServerException: " + serverEx.GetInfo();
                }
            };
            await Task.Run(() =>
            {
                transferManager.Upload(uploadTask);
            });
            return responseModel;
        }
```



查询对象列表

```c#
        public async Task<ResponseModel> SelectObjectList()
        {
            try
            {
                string bucket = _buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                GetBucketRequest request = new GetBucketRequest(bucket);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //执行请求
                GetBucketResult result = await Task.FromResult(_cosXml.GetBucket(request));
                return new ResponseModel { Code = 200, Data = result.GetResultInfo() };
            }
            catch (CosClientException clientEx)
            {
                return new ResponseModel { Code = 0, Data = "CosClientException: " + clientEx.Message };
            }
            catch (CosServerException serverEx)
            {
                return new ResponseModel { Code = 0, Data = "CosServerException: " + serverEx.GetInfo() };
            }
        }
```

下载对象 、删除对象

```c#
        public async Task<ResponseModel> DownObject(string key, string localDir, string localFileName)
        {
            try
            {
                string bucket = _buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                GetObjectRequest request = new GetObjectRequest(bucket, key, localDir, localFileName);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //设置进度回调
                request.SetCosProgressCallback(delegate (long completed, long total)
                {
                    Console.WriteLine(String.Format("progress = {0:##.##}%", completed * 100.0 / total));
                });
                //执行请求
                GetObjectResult result = await Task.FromResult(_cosXml.GetObject(request));

                return new ResponseModel { Code = 200, Message = result.GetResultInfo() };
            }
            catch (CosClientException clientEx)
            {
                return new ResponseModel { Code = 0, Message = "CosClientException: " + clientEx.Message };
            }
            catch (CosServerException serverEx)
            {
                return new ResponseModel { Code = 0, Message = serverEx.GetInfo() };
            }
        }
        public async Task<ResponseModel> DeleteObject(string buketName)
        {
            try
            {
                string bucket = _buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                string key = "exampleobject"; //对象在存储桶中的位置，即称对象键.
                DeleteObjectRequest request = new DeleteObjectRequest(bucket, key);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //执行请求
                DeleteObjectResult result = await Task.FromResult(_cosXml.DeleteObject(request));

                return new ResponseModel { Code = 200, Message = result.GetResultInfo() };
            }
            catch (CosClientException clientEx)
            {
                return new ResponseModel { Code = 0, Message = "CosClientException: " + clientEx.Message };
            }
            catch (CosServerException serverEx)
            {
                return new ResponseModel { Code = 0, Message = "CosServerException: " + serverEx.GetInfo() };
            }
        }
```



以上代码将官方示例作了优化。

- 依赖于抽象、实现接口；
- 松耦合；
- 异步网络流、异步文件流；
- 统一返回信息；
- 增加匿名委托作方法参数；
- 增加灵活性。





## 三，使用封装好的代码



1，初始化

官网示例文档：

![1565356983(1)](https://obs1.whuanle.cn/2019-08/09/1565356983(1).png)

使用修改后的代码，你可以这样初始化：

```c#
            var cosClient = new CosBuilder()
                .SetAccount("1252707544", "	ap-guangzhou")
                .SetCosXmlServer()
                .SetSecret("AKIDEZohU6AmkeNTVPmedw65Ws462rVxLIpG", "Sn1iFi182jMARcheQ1gYIsGSROE5rSwG")
                .Builder();
```



简单测试代码

```c#
        static async Task Main(string[] args)
        {
            // 构建一个 CoxXmlServer 对象
            var cosClient = new CosBuilder()
                .SetAccount("125x707xx4", "ap-guangzhou")
                .SetCosXmlServer()
                .SetSecret("AKIxxxxxxedw65Ws462rVxLIpG", "Sn1iFi1xxxxxwG")
                .Builder();
            // 创建Cos连接客户端
            ICosClient client = new CosClient(cosClient, "125xx0xx44");
            // 创建一个存储桶
            var result = await client.CreateBucket("fsdgerer");
            Console.WriteLine("处理结果：" + result.Message);
            // 查询存储桶列表
            var c = await client.SelectBucket();
            Console.WriteLine(c.Message + c.Data);

            Console.ReadKey();
        }
```



运行结果(部分重要信息使用xx屏蔽)：

```shell
处理结果：200 OK
Connection: keep-alive
Date: Fri, 09 Aug 2019 14:15:00 GMT
Server: tencent-cos
x-cos-request-id: xxxxxxxx=
Content-Length: 0

Success200 OK
Connection: keep-alive
Date: Fri, 09 Aug 2019 14:15:01 GMT
Server: tencent-cos
x-cos-request-id: xxxxxxx=
Content-Type: application/xml
Content-Length: 479

{ListAllMyBuckets:
{Owner:
ID:qcs::cam::uin/1586xx146:uin/158xxx2146
DisPlayName:158x2146
}
Buckets:
{Bucket:
Name:fsdgerer-125xxx7544
Location:ap-guangzhou
CreateDate:
}
{Bucket:
Name:work-1252xxx7544
Location:ap-guangzhou
CreateDate:
}
}
}

```

其它不再赘述