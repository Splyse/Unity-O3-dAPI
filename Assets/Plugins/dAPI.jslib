mergeInto(LibraryManager.library, {

  // pass the API call name, parameters and request id via JSON string
  // { "name": "getAccount", "config": "{json string with parameters for call}", "reqid": "1" }
  dAPICall: function(jparams) { BrowserdAPICall(Pointer_stringify(jparams)) },

  // call this once at start
  StartEventListener: function() {
    window.addEventListener('message',function(event) {
      if ("requestId" in event.data)
      {
        var resp = JSON.stringify(event.data);
        console.log('jslib response recv: ' + resp);
        SendMessage('O3Connector', 'dAPIResponseHandler', resp);
      }
      else if ("eventType" in event.data)
      {
        var resp = JSON.stringify(event.data);
        console.log('jslib event recv: ' + resp);
        SendMessage('O3Connector', 'dAPIEventHandler', resp);
    
      }
    },false);
    // start the dAPI interface in the browser
    initdAPI();
  }
});
