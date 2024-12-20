using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Leaderboards;
using Unity.Services.CloudSave.Internal;
using Unity.Services.Core;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudCode;
using Newtonsoft.Json;

public class UGSManager : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI userIdTmp;

    [SerializeField]
    TextMeshProUGUI ssalTmp;

    [SerializeField]
    TextMeshProUGUI gachaTicketTmp;

    [SerializeField]
    TextMeshProUGUI playerDataTmp;

    LevelData levelData;
    List<PlayersInventoryItem> items = new List<PlayersInventoryItem>();
    async void Start()
    {
        await UnityServices.InitializeAsync(); //전체적인 이니셜라이즈!
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //게스트 로그인의 구현, 이 기기로 로그인 하면 같은 계정으로 쳐요~!

        if (AuthenticationService.Instance.IsSignedIn)
        {
            userIdTmp.text = "Player Id: " + AuthenticationService.Instance.PlayerId;

            await AuthenticationService.Instance.UpdatePlayerNameAsync("pipirongcha");

            LoadFromCloud();
            UpdatePlayerInfo();

            
            CallCloudFunction();
        }
        else
        {
            Debug.Log("로그인 실패");
            //todo: 로그인 안됐으니 초기화면으로 보내버리기
        }
    }

    async void CallCloudFunction()
    {
        try
        {
            var module = new CloudCodeReferenceBindings(CloudCodeService.Instance);
            var result = await module.SayHello(" World");

            Debug.Log("Cloud Code result: " + result);
        }

        catch (CloudCodeException e)
        {
            Debug.Log(e.Message);
        }
    }

    async void UpdatePlayerInfo() //async 잘 붙여주기~
    {
        GetBalancesResult result = await EconomyService.Instance.PlayerBalances.GetBalancesAsync(); //유저들이 가지고 있는 재화들을 가져옴

        foreach (var balance in result.Balances)
        {
            Debug.Log(balance.CurrencyId + ": " + balance.Balance);
        }

        long ssal = result.Balances.Single(balance => balance.CurrencyId == "SSAL").Balance; //LeaderBoard 테스트를 위한 임시 코드
        ssalTmp.text = ssal.ToString(); //LeaderBoard 테스트를 위한 임시 코드
        AddScore(ssal); //LeaderBoard 테스트를 위한 임시 코드

        //ssalTmp.text = result.Balances.Single(balance => balance.CurrencyId == "SSAL").Balance.ToString();

        await FetchAllInventoryItems();

        //Items.Clear();
        //GetInventoryOptions options = new GetInventoryOptions
        //{
        //    ItemsPerFetch = 40
        //};
        //var invenResult = await EconomyService.Instance.PlayerInventory.GetInventoryAsync();
        //Items.AddRange(invenResult.PlayersInventoryItems);

        int counter = 0;
        foreach (var item in items)
        {
            if (item.InventoryItemId == "GACHATICKET")
            {
                counter++;
            }
        }

        gachaTicketTmp.text = counter.ToString();
    }

    async Task FetchAllInventoryItems() //Task로 타입 설정해주기~
    {
        items.Clear();
        GetInventoryOptions options = new GetInventoryOptions
        {
            ItemsPerFetch = 20 //처음엔 20개를 가져오되
        };


        try
        {
            var invenResult = await EconomyService.Instance.PlayerInventory.GetInventoryAsync();
            items.AddRange(invenResult.PlayersInventoryItems);
            while (invenResult.HasNext) //HasNext가 없을때까지 반복
            {
                invenResult = await invenResult.GetNextAsync(); //HasNext가 있으면 뒷부분도 계속 가져오게 하기
                items.AddRange(invenResult.PlayersInventoryItems);
            }
        }
        catch (RequestFailedException e)
        {
            Debug.Log(e);
        }

    }
    public void GetSsalPressed()
    {
        AddSsal(100);
    }

    async void AddSsal(int amount)
    {
        GetBalancesResult result = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
        long currentSsal = result.Balances.Single(balance => balance.CurrencyId == "SSAL").Balance;

        await EconomyService.Instance.PlayerBalances.SetBalanceAsync("SSAL", currentSsal + amount);

        UpdatePlayerInfo();
    }

    public async void PurchaseGachaTicket()
    {
        try
        {
            MakeVirtualPurchaseResult result = await EconomyService.Instance.Purchases.MakeVirtualPurchaseAsync("SSALTOTICKET");
        }
        catch (EconomyException e)
        {
            Debug.Log(e.ToString());
        }

        UpdatePlayerInfo();
    }

    async void LoadFromCloud()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAllAsync();

            if (data.Count == 0)
            {
                SaveToCloud();
            }
            else
            {

                data.TryGetValue("Player", out var playerDataJson);

                levelData = playerDataJson.Value.GetAs<LevelData>();
                UpdateLevelData();
            }
        }
        catch (Exception e)
        {

        }
    }

    async void SaveToCloud()
    {
        if (levelData == null)
        {
            levelData = new LevelData
            {
                level = 1,
                exp = 0
            };
            UpdateLevelData();
        }

        var data = new Dictionary<string, object>
        {
            {"Player" , levelData}
        };

        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }
        catch (CloudSaveException e)
        {
            Debug.Log(e.ToString());
        }

    }

    void UpdateLevelData()
    {
        playerDataTmp.text = "Player Level : " + levelData.level + "\nEXP : " + levelData.exp;
    }

    public async void AddScore(long ssal)
    {
        var scoreResponse = await LeaderboardsService.Instance.AddPlayerScoreAsync("SSALRANKING", ssal);

        GetScores();
    }

    public async void GetScores()
    {
        var scoreResponse = await LeaderboardsService.Instance.GetScoresAsync("SSALRANKING");
        Debug.Log(JsonConvert.SerializeObject(scoreResponse));
    }

}

