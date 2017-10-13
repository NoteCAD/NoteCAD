mergeInto(LibraryManager.library, {
  SaveData: function(data, fileName) {
    var saveDataFunc = (function () {
        var a = document.createElement("a");
        document.body.appendChild(a);
        a.style = "display: none";
        return function (data, fileName) {
            var blob = new Blob([data], {type: "octet/stream"}),
                url = window.URL.createObjectURL(blob);
            a.href = url;
            a.download = fileName;
            a.click();
            window.URL.revokeObjectURL(url);
        };
    }());
    saveDataFunc(Pointer_stringify(data), Pointer_stringify(fileName));
  },
  LoadDataInternal: function() {
    console.log("log 1");
    if (!document.getElementById('FileUploadingPluginInput'))
          Init();
    document.getElementById('gameContainer').addEventListener('click', openFileDialog, false);

    function Init() {
        var inputFile = document.createElement('input');
        inputFile.setAttribute('type', 'file');
        inputFile.setAttribute('id', 'FileUploadingPluginInput');
     
        //filter certain files of type with following line:
        //inputFile.setAttribute('accept', 'image/*'); //or accept="audio/mp3"
     
        inputFile.style.visibility = 'hidden';
     
        inputFile.onclick = function (event) {
            console.log("log 3");
            this.value=null;
        };

        inputFile.onchange = function (evt) {
            //process file
            console.log("log 4");
            evt.stopPropagation();
            var fileInput = evt.target.files;
            if (!fileInput || !fileInput.length) {
                return; // "no selected files"
            }

            var reader = new FileReader();
            reader.readAsText(fileInput[0]);
            
            reader.onload = function() {
              var buffer = _malloc(lengthBytesUTF8(reader.result) + 1);
              writeStringToMemory(reader.result, buffer);
              //callbackForBuffer(buffer);
              SendMessage('JSUtils', 'LoadDataCallback', reader.result);
            }
        }
        document.body.appendChild(inputFile);
    }

    function openFileDialog() {
            console.log("log 2");
            document.getElementById('gameContainer').removeEventListener('click', openFileDialog);
            document.getElementById('FileUploadingPluginInput').click();
            
    }

  }
});


