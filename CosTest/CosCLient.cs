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
using System.Threading.Tasks;

namespace CosTest
{
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
    /// <summary>
    /// Cos客户端
    /// </summary>
    public class CosClient : ICosClient
    {
        CosXmlServer _cosXml;
        private readonly string _appid;
        public CosClient(CosXmlServer cosXml, string appid)
        {
            _cosXml = cosXml;
            _appid = appid;
        }
        public async Task<ResponseModel> CreateBucket(string buketName)
        {
            try
            {
                string bucket = buketName + "-" + _appid; //存储桶名称 格式：BucketName-APPID
                PutBucketRequest request = new PutBucketRequest(bucket);
                //设置签名有效时长
                request.SetSign(TimeUtils.GetCurrentTime(TimeUnit.SECONDS), 600);
                //执行请求
                PutBucketResult result = await Task.FromResult(_cosXml.PutBucket(request));

                return new ResponseModel { Code = 200, Message = result.GetResultInfo() };
            }
            catch (CosClientException clientEx)
            {
                return new ResponseModel { Code = 0, Message = "CosClientException: " + clientEx.Message +clientEx.InnerException};
            }
            catch (CosServerException serverEx)
            {
                return new ResponseModel { Code = 200, Message = "CosServerException: " + serverEx.GetInfo() };
            }
        }
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

    }

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
    }

    /// <summary>
    /// 消息响应
    /// </summary>
    public class ResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public dynamic Data { get; set; }
    }
}

