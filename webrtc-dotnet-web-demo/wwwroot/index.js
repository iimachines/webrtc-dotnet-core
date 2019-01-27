'use strict';
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
const video = document.querySelector('video');
const logElem = document.getElementById('log');
function log(text) {
    console.log(text);
    const line = document.createElement("pre");
    line.innerText = text;
    logElem.appendChild(line);
}
video.addEventListener("readystatechange", () => {
    log(`Video ready state = ${video.readyState}`);
});
video.oncanplay = (e) => __awaiter(this, void 0, void 0, function* () {
    log(`Video can play`);
    //try {
    //    await video.play();
    //} catch (err) {
    //    log(`Video failed to play: ${err}`);
    //}
});
function getSignalingSocketUrl() {
    const scheme = location.protocol === "https:" ? "wss" : "ws";
    const port = location.port ? (":" + location.port) : "";
    const url = scheme + "://" + location.hostname + port + "/signaling";
    return url;
}
const pc = new RTCPeerConnection();
const ws = new WebSocket(getSignalingSocketUrl());
ws.binaryType = "arraybuffer";
function send(action, payload) {
    const msg = JSON.stringify({ action, payload });
    log(`send ${msg}`);
    ws.send(msg);
}
ws.onopen = e => {
    pc.onicecandidate = e => {
        send("ice", e.candidate);
    };
    pc.oniceconnectionstatechange = e => {
        log(`ice connection state = ${pc.iceConnectionState}`);
    };
    pc.ontrack = e => {
        const stream = e.streams[0];
        if (video.srcObject !== stream) {
            video.srcObject = stream;
            video.play();
            log(`✔: received media stream`);
        }
    };
};
ws.onmessage = (e) => __awaiter(this, void 0, void 0, function* () {
    const { action, payload } = JSON.parse(e.data);
    log(`receive: ${e.data}`);
    try {
        switch (action) {
            case "ice": {
                yield pc.addIceCandidate(payload);
                log(`✔: addIceCandidate`);
                break;
            }
            case "sdp": {
                yield pc.setRemoteDescription(payload);
                log(`✔: setRemoteDescription`);
                const sdp = yield pc.createAnswer({ offerToReceiveVideo: true });
                log(`✔: createAnswer`);
                yield pc.setLocalDescription(sdp);
                log(`✔: setLocalDescription`);
                send("sdp", sdp);
            }
        }
    }
    catch (err) {
        log(err);
    }
});
//# sourceMappingURL=index.js.map