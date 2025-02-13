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
    prefabs�� �������� ���� ���� ����� Dictionary
    ������ ������ ���� ������ �ν����Ϳ��� �Է��� ���� �ʴ´ٴ� ��...
    ������ ���� ����, ���� ���Ÿ� �������� scriptable object�� ����ϱ�� �ߴ�.
    */
    [SerializeField] CharacterPrefab[] prefabs;

    
    bool isWaiting; //�̱� ���� ����� ����
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
        if (Input.GetKeyDown(KeyCode.Escape))   // ����� �����̶��
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
        //�ִϸ��̼��� ������ �ڷ�ƾ (���� ������ ��ٸ��� �ִϸ��̼��� ��ȯ�ϴ�)
        StartCoroutine(WaitGachaRoutine());
        
        //���� ���� ��û�ϴ� �ڵ�
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

    //���� �̱Ⱑ �������� ��
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