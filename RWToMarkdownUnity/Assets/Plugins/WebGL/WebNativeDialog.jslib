var WebNativeDialog = {
  NativeDialogPrompt:function (title , defaultValue){
    defaultValue = UTF8ToString(defaultValue);
    title = UTF8ToString(title);
    var result = window.prompt( title , defaultValue );
    if( !result ){
      result = defaultValue;
    }
    var buffer = _malloc(lengthBytesUTF8(result) + 1);
    writeStringToMemory(result, buffer);
    return buffer;
  },
  SetupOverlayDialogHtml:function(title,defaultValue,okBtnText){
    title = UTF8ToString(title);
    defaultValue = UTF8ToString(defaultValue);
    okBtnText = UTF8ToString(okBtnText);

    if( !document.getElementById("nativeInputDialogInput" ) ){
      // setup css
      var style = document.createElement( 'style' );
      style.setAttribute('id' , 'inputDialogTextSelect');
      style.appendChild( document.createTextNode( '#nativeInputDialogInput::-moz-selection { background-color:#00ffff;}' ) );
      style.appendChild( document.createTextNode( '#nativeInputDialogInput::selection { background-color:#00ffff;}' ) );
      document.head.appendChild( style );
    }
    if( !document.getElementById("nativeInputDialog" ) ){
      // setup html
      var html = '<div id="nativeInputDialog" style="background:#000000;opacity:0.9;width:100%;height:100%;position:fixed;top:0%;z-index:2147483647;">' + 
               '  <div style="position:relative;top:30%;" align="center" vertical-align="middle">' + 
               '    <div id="nativeInputDialogTitle" style="color:#ffffff;">Here is title</div>' + 
               '    <div>' + 
               '      <textarea id="nativeInputDialogInput" style="margin: 0px; width: 650px; height: 400px;"></textarea>' + 
               '    </div>' + 
               '    <div style="margin-top:10px">' + 
               '      <input id="nativeInputDialogOkBtn" type="button" value="OK" onclick="" >' + 
               '      <input id="nativeInputDialogCopyBtn" type="button" value="Copy to clipboard" onclick="" >' + 
               '      <input id="nativeInputDialogCheck" type="checkBox" style="display:none;">' + 
               '    </div>' + 
               '  </div>' + 
               '</div>';
      var element = document.createElement('div');
      element.innerHTML = html;
      // write to html
      document.body.appendChild( element );

      // set Event
		var closeFunction = 
        'document.getElementById("nativeInputDialog" ).style.display = "none";';

      var copyFunction = 
        'var copyText = document.getElementById("nativeInputDialogInput");' +
		'copyText.select();' +
		'document.execCommand("copy");' +
		'alert("Copied the text!");';
		

      var inputField = document.getElementById("nativeInputDialogInput");
      //inputField.setAttribute( "onsubmit" , okFunction );
      var okBtn = document.getElementById("nativeInputDialogOkBtn");
      okBtn.setAttribute( "onclick" , closeFunction);
      var copyBtn = document.getElementById("nativeInputDialogCopyBtn");
      copyBtn.setAttribute( "onclick" , copyFunction);
	  
    }
    document.getElementById("nativeInputDialogTitle").innerText = title;
    document.getElementById("nativeInputDialogInput").value= defaultValue;

    document.getElementById("nativeInputDialogOkBtn").value = okBtnText;
    document.getElementById("nativeInputDialog" ).style.display = "";
  },
  HideUnityScreenIfHtmlOverlayCant:function(){
    if( navigator.userAgent.indexOf("Chrome/") < 0 ){
      document.getElementById("canvas").style.display="none";
    }
  },
  IsRunningOnEdgeBrowser:function(){
    if( navigator.userAgent.indexOf("Edge/") < 0 ){
      return false;
    }
    return true;
  },
  IsOverlayDialogHtmlActive:function(){
    var nativeDialog = document.getElementById("nativeInputDialog" );
    if( !nativeDialog ){
      return false;
    }
    return ( nativeDialog.style.display != 'none' );
  },
  IsOverlayDialogHtmlCanceled:function(){

  },
  GetOverlayHtmlInputFieldValue:function(){
    var inputField = document.getElementById("nativeInputDialogInput");
    var result = "";
    if( inputField && inputField.value ){
      result = inputField.value;
    }
    var buffer = _malloc(lengthBytesUTF8(result) + 1);
    writeStringToMemory(result, buffer);
    return buffer;
  }

};
mergeInto( LibraryManager.library , WebNativeDialog );

