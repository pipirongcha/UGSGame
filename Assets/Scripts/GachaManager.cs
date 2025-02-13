using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GachaManager : MonoBehaviour
{
    [SerializeField] GameObject gate;
    [SerializeField] Transform characterParent;
    [SerializeField] GameObject blockCanvas;

    /*
    prefabs를 가져오는 가장 좋은 방법은 Dictionary
    문제는 오딘을 깔지 않으면 인스펙터에서 입력이 되지 않는다는 것...
    오딘은 유료 에셋, 아직 구매를 안했으니 scriptable object를 사용하기로 했다.
    */
    [SerializeField] CharacterPrefab[] prefabs;

    
    bool isWaiting; //뽑기 종료 대기중 여부
    string characterGet;

    async void Start()
    {
        isWaiting = false;

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    void Update()
    {
#if UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))   // 모바일 게임이라면
        {
            BackPressed();
        }
#endif
    }

    public void BackPressed()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public async void GachaPressed()
    {
        //애니메이션을 돌리는 코루틴 (서버 응답을 기다리고 애니메이션을 전환하는)
        StartCoroutine(WaitGachaRoutine());
        
        //서버 응답 요청하는 코드
        CallGachaCloudCode();
    }
    IEnumerator WaitGachaRoutine()
    {
        blockCanvas.SetActive(true);
        float timer = 0;

        gate.GetComponent<Animator>().Play("Idle");

        yield return null;

        while (timer < 2 || isWaiting)
        {
            timer += 2;

            gate.GetComponent<Animator>().Play("Shaking");
            yield return new WaitForSeconds(2);
        }

        if (characterParent.childCount > 0)
        {
            Destroy(characterParent.GetChild(0).gameObject);
        }

        foreach (CharacterPrefab prefab in prefabs)
        {
            if (prefab.characterName.Equals(characterGet))
            {
                Instantiate(prefab.prefab, characterParent);
            }
        }

        blockCanvas.SetActive(false);
        gate.GetComponent<Animator>().Play("Opening");

    }

    //실제 뽑기가 행해지는 곳
    async void CallGachaCloudCode()
    {
        isWaiting = true;
        var module = new CloudCodeReferenceBindings(CloudCodeService.Instance);
        var result = await module.GetGacha();

        Debug.Log("cloud code result :" + result);
        characterGet = result;

        isWaiting = false;
    }
}