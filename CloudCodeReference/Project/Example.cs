using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using CloudCodeReference;

namespace HelloWorld;

public class MyModule
{
    ILogger<MyModule> logger;//로그 찍는데 쓰임
    IGameApiClient apiClient; //UGS 기능들 사용에 쓰임

    public MyModule(IGameApiClient apiClient, ILogger<MyModule> logger)
    {
        this.apiClient = apiClient;
        this.logger = logger;
    }

    [CloudCodeFunction("SayHello")]
    public string Hello(string name)
    {
        return $"Hello, {name}!";
    }
    [CloudCodeFunction("GetGacha")] //이렇게 선언해준 것만 밖에서 호출할 수 있다!
    public async Task<string> GetGacha(IExecutionContext context,IGameApiClient apiClient)
    {
        //var result = await apiClient.CloudSaveData.GetItemsAsync(context, context.AccessToken, context.ProjectId, context.PlayerId,
        //new List<string> { "Player" });
        //string saveData = result.Data.Results.First().Value.ToString();

        //logger.LogDebug(saveData);

        //return saveData;

        GachaManager gachaManager = new GachaManager(logger, apiClient);
        string result = await gachaManager.DoGacha(context);
        return result;
    }

    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.Dependencies.AddSingleton(GameApiClient.Create());
        }
    }
}


