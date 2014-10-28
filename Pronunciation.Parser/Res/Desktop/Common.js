var myAudioContext, isWebKit;

// 1 - read mp3 data directly from Base64 string and play via Web Audio API (doesn't work in IE)
// 2 - read mp3 data directly from Base64 string and play via Audio element (doesn't work in mobile browsers)
// 3 - play mp3 file via Audio element
// 4 - pass mp3 data to the container for playing
// 5 - pass mp3 audio key to the container for playing
var audioMode = 5; // use '5' for DB mode and '2' for file system mode (in Chrome)

// *** Functions called by WebBrowser control
//
function extGetAudioByKey(audioKey) {
    var elements = document.getElementsByClassName("audio_button");
    for (var i = 0; i < elements.length; i++) {
        var element = elements[i];

        if (getAudioKey(element) == audioKey)
            return getAudioData(element);
    }
}

function extHiglightAudio(audioKey) {
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

function extResetNotes() {
    var container = document.getElementById("customNotesContainer");
    container.innerHTML = '';
}

function extRefreshNotes(transcription, notes) {
    var html = '<div class="custom_notes">';
    if (transcription) {
        html += '<div class="pron_favorite">' + transcription + '</div>';
    }
    if (notes) {
        html += '<div class="custom_note">' + notes + '</div>';
    }
    html += "</div>";

    var container = document.getElementById("customNotesContainer");
    container.innerHTML = html;
}
// ***********

function registerHandlers() {
    registerAudio();
}

function loadPage(keyword) {
    window.external.LoadPageExt(keyword);
}

function getAudioKey(element) {
    return element.attributes["data-src"].value;
}

function getAudioData(element) {
    return pageAudio[getAudioKey(element)];
}

function getAudioText(element) {
    var attribute = element.attributes["audio_title"];
    return (attribute ? attribute.value : null);
}

function registerAudio() {
    var elements = document.getElementsByClassName("audio_button");
    for (var i = 0; i < elements.length; i++) {
        var element = elements[i];
        element.addEventListener('click', playAudio, false);
    }
}

function playAudio() {
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
        case 4:
            func = rawDataViaContainer;
            break;
        case 5:
            func = audioKeyViaContainer;
            break;
    }

    func(this);
    extHiglightAudio(getAudioKey(this));
}

function rawDataViaApi(element) {
    var rawData = getAudioData(element);
    if (!rawData)
        return;

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
        var arrayBuff = Base64Binary.decodeArrayBuffer(rawData);
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

function rawDataViaAudio(element) {
    var rawData = getAudioData(element);
    if (!rawData)
        return;

    var audio = new Audio("data:audio/mpeg;base64," + rawData);
    audio.addEventListener("error", function (e) { alert(this.error.code); });
    audio.play();
}

function rawDataViaContainer(element) {
    playAudioViaContainer(element, true);
}

function audioKeyViaContainer(element) {
    playAudioViaContainer(element, false);
}

function playAudioViaContainer(element, loadData) {
    var audioKey = getAudioKey(element);
    if (!audioKey)
        return;

    var rawData = null;
    if (loadData) {
        rawData = getAudioData(element);
        if (!rawData)
            return;
    }

    window.external.PlayAudioExt(audioKey, getAudioText(element), rawData);
}

function fileViaAudio(element) {
    var audioKey = getAudioKey(element);
    if (!audioKey)
        return;

    var filePath = '../../Sounds/' + audioKey + '.mp3';
    var audio = new Audio(filePath);
    audio.addEventListener("error", function (e) { alert(this.error.code); });
    audio.play();
}

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