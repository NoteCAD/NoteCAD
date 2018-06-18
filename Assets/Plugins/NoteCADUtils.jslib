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
            this.value=null;
        };

        inputFile.onchange = function (evt) {
            //process file
            evt.stopPropagation();
            var fileInput = evt.target.files;
            if (!fileInput || !fileInput.length) {
                return; // "no selected files"
            }

            var reader = new FileReader();
            reader.readAsText(fileInput[0]);
            
            reader.onload = function() {
              SendMessage('JSUtils', 'LoadDataCallback', reader.result);
            }
        }
        document.body.appendChild(inputFile);
    }

    function openFileDialog() {
            document.getElementById('gameContainer').removeEventListener('click', openFileDialog);
            document.getElementById('FileUploadingPluginInput').click();
            
    }
  },
  GetParam: function (param) {
    var vars = {};
    window.location.href.replace( location.hash, '' ).replace( 
        /[?&]+([^=&]+)=?([^&]*)?/gi, // regexp
        function( m, key, value ) { // callback
            vars[key] = value !== undefined ? value : '';
        }
    );
    var result = vars[Pointer_stringify(param)];
    if(result === undefined) result = '';
    var bufferSize = lengthBytesUTF8(result) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(result, buffer, bufferSize);
    return buffer;
  },
  LoadBinaryDataInternal: function() {
    if (!document.getElementById('BinaryFileInput')) {
      var fileInput = document.createElement('input');
      fileInput.setAttribute('type', 'file');
      fileInput.setAttribute('id', 'BinaryFileInput');
      fileInput.style.visibility = 'hidden';
      fileInput.onclick = function (event) {
        this.value = null;
      };
      fileInput.onchange = function (event) {
        SendMessage('JSUtils', 'BinaryFileSelected', URL.createObjectURL(event.target.files[0]));
      }
      document.body.appendChild(fileInput);
    }
    var OpenFileDialog = function() {
      document.getElementById('BinaryFileInput').click();
      document.getElementById('gameContainer').removeEventListener('click', OpenFileDialog);
    };
    document.getElementById('gameContainer').addEventListener('click', OpenFileDialog, false);
  }
});


