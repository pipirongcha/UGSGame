using HelloWorld;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CloudCodeReference.GachaManager;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;
namespace CloudCodeReference
{
    internal class GachaManager
    {
        ILogger<MyModule> logger;
        IGameApiClient apiClient;

        [Serializable]
        internal class GachaItem
        {
            public string Name { get; set; }
            public int Factor { get; set; }
        }

        internal class CharacterOwned
        {
            public string Name { get; set; }
            public int Level { get; set; }
        }
        public GachaManager(ILogger<MyModule> logger, IGameApiClient apiClient)
        {
            this.logger = logger;
            this.apiClient = apiClient;
        }

        public async Task<string> DoGacha(IExecutionContext context)
        {
            var result = apiClient.RemoteConfigSettings.AssignSettingsGetAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.EnvironmentId,
                null,
                new List<string> { "Gacha" });

            List<GachaItem> items = JsonConvert.DeserializeObject<List<GachaItem>>(
                result.Result.Data.Configs.Settings["Gacha"].ToString());

            int totalFactor = items.Sum(item => item.Factor);

            string selectedName = "";
            Random random = new Random();
            int randomValue = random.Next(totalFactor);
            int countWeight = 0;

            foreach (GachaItem item in items)
            {
                countWeight += item.Factor;
                if (randomValue < countWeight)
                {
                    selectedName = item.Name;
                    break;
                }
            }



            //세이브 파일 존재여부 확인
            var characterSaved = await apiClient.CloudSaveData.GetItemsAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId,
                new List<string>
                {
                    "CharacterOwned"
                });

            List<CharacterOwned> characters = new List<CharacterOwned>();


            if (characterSaved.Data.Results.Count == 0)
            {
                characters.Add(new CharacterOwned
                {
                    Name = selectedName,
                    Level = 1,
                });
            }
            else
            {
                var savedData = characterSaved.Data.Results.FirstOrDefault(item => item.Key == "CharacterOwned");
                if (savedData != null)
                {
                    //CharacterOwned라는 Key에 저장되어있던 캐릭터 목록이 Charaters로 들어오게끔 함 => Json을 Deserialize하는 방법으로
                    characters = JsonConvert.DeserializeObject<List<CharacterOwned>>(savedData.Value.ToString());
                    var existingCharacter = characters.FirstOrDefault(c => c.Name == selectedName);
                    if (existingCharacter != null)
                    {
                        existingCharacter.Level += 1;
                    }
                    else
                    {
                        characters.Add(new CharacterOwned
                        {
                            Name = selectedName,
                            Level = 1
                        });
                    }

                }

                
            }
            await apiClient.CloudSaveData.SetItemAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId,
                    new SetItemBody("CharacterOwned", JsonConvert.SerializeObject(characters)));
            return selectedName;
        }


    }
}
