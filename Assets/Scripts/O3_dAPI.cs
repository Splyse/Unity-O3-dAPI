using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class O3_dAPI : MonoBehaviour
{
    public TextMesh welcomeText;
    public TextMesh providerText;
    public TextMesh mctBalanceText;
    public TextMesh rhtBalanceText;
    public TextMesh supplyText;
    public TextMesh messageText;
    public TextMesh errorText;

    private string Address;
    private string PubKey;
    private string MCTBalance;
    private string RHTBalance;
    private string MCTHash = "a87cc2a513f5d8b4a42432343687c2127c60bc3f";
    private string RHTHash = "2328008e6f6c7bd157a342e789389eb034d9cbc4";
    private string Provider;
    private string Circulation;
    private string RHTName;
    private string TXID;
    private bool isReady = false;

    // dAPI call wrapper
    [DllImport("__Internal")]
    private static extern void dAPICall(string jparams);

    // message listener that passes data to callbacks
    [DllImport("__Internal")]
    private static extern void StartEventListener();

    private int _requestCount = 0;
    private Dictionary<string, string> ResultQueue;

    void Start()
    {
        ResultQueue = new Dictionary<string, string>();
        errorText.GetComponent<FadeText>().FadeIn("Waiting for dAPI-enabled wallet to be connected...");

#if UNITY_WEBGL && !UNITY_EDITOR
        StartEventListener();
        StartCoroutine(GetInfo());
#else
        Debug.LogError("Must run in WebGL connecting to a running O3 client");
#endif

    }

    public IEnumerator GetInfo()
    {
        yield return new WaitUntil(() => isReady);
        errorText.text = "";

        yield return SendRequest("getProvider", (result) => { Provider = JObject.Parse(result)["name"].ToString(); });
        providerText.GetComponent<FadeText>().FadeIn($"Connected to: {Provider}");

        yield return SendRequest("getAccount", (result) => { Address = JObject.Parse(result)["address"].ToString(); });
        welcomeText.GetComponent<FadeText>().FadeIn($"Welcome to Unity, {Address}");

        yield return SendRequest("getBalance", new { @params = new { assets = new string[] { MCTHash }, address = Address }, 
            network = "MainNet" }, (result) => { MCTBalance = JObject.Parse(result)[Address][0]["amount"].ToString(); });
        mctBalanceText.GetComponent<FadeText>().FadeIn($"MCT Balance: {MCTBalance}");

        yield return SendRequest("getBalance", new { @params = new { assets = new string[] { RHTHash }, address = Address },
            network = "MainNet" }, (result) => { RHTBalance = JObject.Parse(result)[Address][0]["amount"].ToString(); });
        rhtBalanceText.GetComponent<FadeText>().FadeIn($"RHT Balance: {RHTBalance}");

        yield return SendRequest("getStorage", new { scriptHash = RHTHash, key = "696e5f63697263756c6174696f6e", network = "MainNet" },
            (res) => { Circulation = HexToInt(JObject.Parse(res)["result"].ToString()).ToString(); });

        yield return SendRequest("invokeRead", new {
            scriptHash = RHTHash,
            operation = "name",
            arguments = new string[] { },
            network = "MainNet" },
            (result) => { RHTName = Encoding.ASCII.GetString(StringToByteArray(JObject.Parse(result)["stack"][0]["value"].ToString())); });
        supplyText.GetComponent<FadeText>().FadeIn($"Total {RHTName} supply: {Circulation}");

        yield return SendRequest("send", new {
            fromAddress = Address,
            toAddress = Address,
            asset = MCTHash,
            amount = "0.00000001",
            remark = "Sending a drop of MCT to myself",
            fee = "0",
            network = "MainNet"
        }, (result) => { TXID = JObject.Parse(result)["txid"].ToString(); });

        messageText.GetComponent<FadeText>().FadeIn($"Initiated MCT transfer in TX {TXID}");

        if (MCTBalance == "0")
        {
            errorText.GetComponent<FadeText>().FadeIn($"{Address} has zero MCT, transfer will fail");
        }

    }

    public IEnumerator SendRequest(string request, Action<string> callback)
    {
        yield return SendRequest(request, "", callback);
    }

    public IEnumerator SendRequest(string request, object oparams, Action<string> callback)
    {
        string jparams = JsonConvert.SerializeObject(oparams);
        yield return SendRequest(request, jparams, callback);
    }

    public IEnumerator SendRequest(string request, string jparams, Action<string> callback)
    {
        _requestCount += 1;
        string reqid = _requestCount.ToString();
        Debug.Log($"Sending request {reqid}: {request}({jparams})");
        if (jparams == "")
        {
            dAPIRequest req = new dAPIRequest(request, reqid);
            dAPICall(JsonUtility.ToJson(req));
        }
        else
        {
            dAPIRequestWithParameters req = new dAPIRequestWithParameters(request, jparams, reqid);
            dAPICall(JsonUtility.ToJson(req));
        }
        yield return new WaitUntil(() => ResultQueue.ContainsKey(reqid));
        string result = ResultQueue[reqid];
        ResultQueue.Remove(reqid);
        callback(result);
    }

    public void dAPIResponseHandler(string jresponse)
    {
        dAPIResult response = JsonUtility.FromJson<dAPIResult>(jresponse);
        if (response.errorState)
        {
            Debug.LogError($"Request {response.requestId} failed: {response.resultData}");
            ResultQueue.Add(response.requestId, "");
            errorText.GetComponent<FadeText>().FadeIn(response.resultData);
        }
        else
        {
            ResultQueue.Add(response.requestId, response.resultData);
        }
    }

    public void dAPIEventHandler(string jresponse)
    {
        dAPIEvent dapievent = JsonUtility.FromJson<dAPIEvent>(jresponse);
        if (dapievent.eventType == "READY")
        {
            isReady = true;
        }
        else if (dapievent.eventType == "DISCONNECTED")
        {
            errorText.text = "dAPI-enabled wallet disconnected!";
        }
        else if (dapievent.eventType == "ACCOUNT_CHANGED")
        {
            Address = JObject.Parse(dapievent.eventData)["address"].ToString();
            welcomeText.GetComponent<FadeText>().FadeIn($"Welcome to Unity, {Address}");
        }
        else
        {
            Debug.Log($"Unhandled event {dapievent.eventType}");
        }
    }

    private ulong HexToInt(string hex)
    {
       return BitConverter.ToUInt64(StringToByteArray(hex.PadRight(16, '0')), 0);
    }

    private byte[] StringToByteArray(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        int bl = bytes.Length;
        for (int i = 0; i < bl; ++i)
        {
            bytes[i] = (byte)((hex[2 * i] > 'F' ? hex[2 * i] - 0x57 : hex[2 * i] > '9' ? hex[2 * i] - 0x37 : hex[2 * i] - 0x30) << 4);
            bytes[i] |= (byte)(hex[2 * i + 1] > 'F' ? hex[2 * i + 1] - 0x57 : hex[2 * i + 1] > '9' ? hex[2 * i + 1] - 0x37 : hex[2 * i + 1] - 0x30);
        }
        return bytes;
    }

}

public class dAPIEvent
{
    public string eventType;
    public string eventData;

    public dAPIEvent(string _type, string _data)
    {
        eventType  = _type;
        eventData = _data;
    }
}

public class dAPIResult
{
    public string requestId;
    public string resultData;
    public bool errorState;

    public dAPIResult(string _id, string _data, bool _state)
    {
        requestId = _id;
        resultData = _data;
        errorState = _state;
    }
}

public class dAPIRequest
{
    public string name;
    public string reqid;

    public dAPIRequest(string _name, string _reqid)
    {
        name = _name;
        reqid = _reqid;
    }
}

public class dAPIRequestWithParameters
{
    public string name;
    public string config;
    public string reqid;

    public dAPIRequestWithParameters(string _name, string _config, string _reqid)
    {
        name = _name;
        config = _config;
        reqid = _reqid;
    }
}