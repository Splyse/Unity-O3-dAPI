let dapi;

function initdAPI()
{
  o3dapi.initPlugins([o3dapiNeo]);
  dapi = window.o3dapi.NEO;

  o3dapi.NEO.addEventListener(o3dapi.NEO.Constants.EventName.READY, 
    data => {
      console.log(`dAPI provider ready: ${data.name}`);
      postEvent("READY", JSON.stringify(data));
  });

  o3dapi.NEO.addEventListener(o3dapi.NEO.Constants.EventName.ACCOUNT_CHANGED, 
    data => {
      console.log(`Changed Account: ${data.address}`);
      postEvent("ACCOUNT_CHANGED", JSON.stringify(data));
  });

  o3dapi.NEO.addEventListener(o3dapi.NEO.Constants.EventName.CONNECTED, 
    data => {
      console.log(`Connected Account: ${data.address}`);
      postEvent("CONNECTED", JSON.stringify(data));
  });

  o3dapi.NEO.addEventListener(o3dapi.NEO.Constants.EventName.DISCONNECTED, 
    data => {
      console.log(`dAPI provider disconnected`);
      postEvent("DISCONNECTED", "{}");
  });

  o3dapi.NEO.addEventListener(o3dapi.NEO.Constants.EventName.NETWORK_CHANGED, 
    data => {
      console.log(`Network changed: ${data.defaultNetwork}`);
      postEvent("NETWORK_CHANGED", JSON.stringify(data));
  });
}

function BrowserdAPICall(jparams) {
  var api = JSON.parse(jparams);
  if (("name" in api) && ("config" in api) && ("reqid" in api))
  { 
    // api call with parameters
    dapi[api.name](JSON.parse(api.config))
    .then((result) => { 
          result = typeof result !== "string"
        ? JSON.stringify(result)
        : result;
       postResult(api.reqid, result, false) })
    .catch((err) => { 
      var error = typeof err !== "string"
        ? `${err.type}: ${err.message}`
        : `dAPI call failed: ${err}`;
      postResult(api.reqid, error, true);
   });
  } else if (("name" in api) && ("reqid" in api))
  { 
    // api call without parameters
    dapi[api.name]()
    .then((result) => { 
          result = typeof result !== "string"
        ? JSON.stringify(result)
        : result;
       postResult(api.reqid, result, false) })
    .catch((err) => { 
      var error = typeof err !== "string"
        ? `${err.type}: ${err.message}`
        : `dAPI call failed: ${err}`;
      postResult(api.reqid, error, true) 
    });
  }
  else if ("reqid" in api)
  {
    postResult(api.reqid, 'Invalid API name', true);
  }
  else 
  {
    postResult('-1', 'Missing request ID', true);
  }
}

function postResult(id, res, state) {
  if ((state) && (res == ""))
  {
    res = "Unknown error";
  }
  var msg = {
    requestId: id,
    resultData: res,
    errorState: state 
  };
  window.postMessage(msg, "*");
}

function postEvent(type, data) {
  var msg = {
    eventType: type,
    eventData: data
  };
  window.postMessage(msg, "*");
}  

