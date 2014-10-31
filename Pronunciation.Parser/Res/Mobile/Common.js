var myAudioContext, isWebKit;
var firstAudioUk, firstAudioUs;
var lastPlayedAudio;

var repeatDelay = 3; // in seconds
var timeoutId = null;

// 1 - read mp3 data directly from Base64 string and play via Web Audio API (doesn't work in IE)
// 2 - read mp3 data directly from Base64 string and play via Audio element (doesn't work in mobile browsers)
// 3 - play mp3 file via Audio element
var audioMode = 1;

function registerHandlers() {
    registerDynamicContent();
    registerAudio();
    registerSubmit();
    adjustOrientation();
}

function registerDynamicContent() {
    var div = document.getElementById('navigationContainer');
    div.innerHTML =
    //    '<div class="navigationRow">Repeat every<input type="text" id="txtInterval"/>seconds.<input type="button" id="btnRecord" value="Start/Stop" /></div>' +
    '<div class="navigationRow">Search:<input type="text" id="txtNavigate" autocapitalize="off" autocorrect="off" /><input type="submit" value="Go"/></div>';

    //    var button = document.getElementById('btnRecord');
    //    button.addEventListener('click', initRecording, false);

    //    var txt = document.getElementById('txtInterval');
    //    txt.value = repeatDelay;
}

function adjustOrientation() {
    var div = document.getElementById('navigationContainer');
    if (window.orientation == 0) {
        div.style.marginTop = "7em";
    } else {
        div.style.marginTop = "4em";
    }
}

function registerAudio() {
    var elements = document.getElementsByClassName("audio_button");
    for (var i = 0; i < elements.length; i++) {
        var element = elements[i];
        element.addEventListener('click', playAudio, false);

        if (firstAudioUk == null || firstAudioUs == null) {
            var className = element.attributes["class"].value;

            if (firstAudioUk == null && className.indexOf("audio_uk") >= 0) {
                firstAudioUk = element;
            }

            if (firstAudioUs == null && className.indexOf("audio_us") >= 0) {
                firstAudioUs = element;
            }
        }
    }
}

function registerSubmit() {
    document.getElementById("mainForm").onsubmit = function () {

        var fileName = document.getElementById("txtNavigate").value.toLowerCase();
        var letter = fileName[0];
        window.location = '../../Dic/' + letter + '/' + fileName + '.html';

        return false;
    }
}

function playAudio() {
    playAudioData(getAudioData(this));
    lastPlayedAudio = this;

    higlightAudio(getAudioKey(this));
}

function playAudioData(data) {
    var func;
    switch (audioMode) {
        case 1:
            func = rawDataViaApi;
            break;
        case 2:
            func = rawDataViaAudio;
            break;
        case 3:
            func = fileViaAudio;
            break;
    }

    func(data);
}

function getAudioKey(element) {
    return element.attributes["data-src"].value;
}

function getAudioData(element) {
    return pageAudio[getAudioKey(element)];
}

function higlightAudio(audioKey) {
    var elements = document.getElementsByClassName("audio_button");
    for (var i = 0; i < elements.length; i++) {
        var element = elements[i];
        if (getAudioKey(element) == audioKey) {
            element.style.color = "red";
        } else {
            element.style.color = "black";
        }
    }
}

function initRecording() {
    // Stop recording if it's in progress
    if (timeoutId) {
        window.clearInterval(timeoutId);
        timeoutId = null;
        return;
    }

    var data;
    if (lastPlayedAudio == null) {
        if (firstAudioUs == null) {
            return;
        }
        data = getAudioData(firstAudioUs);
    } else {
        data = getAudioData(lastPlayedAudio);
    }

    playAudioData(data);

    var interval = document.getElementById('txtInterval').value;
    timeoutId = window.setInterval(playAudioData, (+interval) * 1000, data);
}

function rawDataViaApi(raw_data) {

    if (myAudioContext == null) {
        if ('AudioContext' in window) {
            myAudioContext = new AudioContext();
            isWebKit = false;
        } else if ('webkitAudioContext' in window) {
            myAudioContext = new webkitAudioContext();
            isWebKit = true;
        } else {
            alert('Your browser does not support Web Audio API!');
            return;
        }
    }

    try {
        var arrayBuff = Base64Binary.decodeArrayBuffer(raw_data);
        var mySource = myAudioContext.createBufferSource();
        //        myAudioContext.decodeAudioData(arrayBuff, function (audioData) {
        //            myBuffer = audioData;
        //        });
        mySource.buffer = myAudioContext.createBuffer(arrayBuff, false);
        mySource.connect(myAudioContext.destination);

        if (isWebKit) {
            mySource.noteOn(0);
        } else {
            mySource.start(0);
        }
    }
    catch (e) {
        var msg = e.message;
        if (e.stack) {
            msg += "\r\n" + e.stack;
        }
        alert(msg);
    }
}

function rawDataViaAudio(raw_data) {

    var audio = new Audio("data:audio/mpeg;base64," + raw_data);
    audio.addEventListener("error", function (e) { alert(this.error.code); });
    audio.play();
}

function fileViaAudio(fileName) {
    var file_ref = '../../Sounds/' + fileName + '.mp3';

    var audio = new Audio(file_ref);
    audio.addEventListener("error", function (e) { alert(this.error.code); });
    audio.play();
}

//function playRaw(source, event) {
//    var path = source.attributes["data-src-mp3"].value;
//    window.open(path, "Sound", "menubar=no, status=no, scrollbars=no, menubar=no, width=200, height=100");
//}

var Base64Binary = {
    _keyStr: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=",

    /* will return a  Uint8Array type */
    decodeArrayBuffer: function (input) {
        var bytes = (input.length / 4) * 3;
        var ab = new ArrayBuffer(bytes);
        this.decode(input, ab);

        return ab;
    },

    decode: function (input, arrayBuffer) {
        //get last chars to see if are valid
        var lkey1 = this._keyStr.indexOf(input.charAt(input.length - 1));
        var lkey2 = this._keyStr.indexOf(input.charAt(input.length - 2));

        var bytes = (input.length / 4) * 3;
        if (lkey1 == 64) bytes--; //padding chars, so skip
        if (lkey2 == 64) bytes--; //padding chars, so skip

        var uarray;
        var chr1, chr2, chr3;
        var enc1, enc2, enc3, enc4;
        var i = 0;
        var j = 0;

        if (arrayBuffer)
            uarray = new Uint8Array(arrayBuffer);
        else
            uarray = new Uint8Array(bytes);

        input = input.replace(/[^A-Za-z0-9\+\/\=]/g, "");

        for (i = 0; i < bytes; i += 3) {
            //get the 3 octects in 4 ascii chars
            enc1 = this._keyStr.indexOf(input.charAt(j++));
            enc2 = this._keyStr.indexOf(input.charAt(j++));
            enc3 = this._keyStr.indexOf(input.charAt(j++));
            enc4 = this._keyStr.indexOf(input.charAt(j++));

            chr1 = (enc1 << 2) | (enc2 >> 4);
            chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
            chr3 = ((enc3 & 3) << 6) | enc4;

            uarray[i] = chr1;
            if (enc3 != 64) uarray[i + 1] = chr2;
            if (enc4 != 64) uarray[i + 2] = chr3;
        }

        return uarray;
    }
}