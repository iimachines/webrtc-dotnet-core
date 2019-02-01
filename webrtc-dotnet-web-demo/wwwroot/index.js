'use strict';
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
function main() {
    const video = document.querySelector('video');
    const logElem = document.getElementById('log');
    // Clear log
    logElem.innerText = "";
    // https://github.com/webrtc/samples/blob/gh-pages/src/content/peerconnection/bandwidth/js/main.js
    function removeBandwidthRestriction(sdp) {
        // TODO: Doesn't seem to work...
        return sdp.replace(/b=AS:.*\r\n/, '').replace(/b=TIAS:.*\r\n/, '');
    }
    function log(text) {
        console.log(text);
        const line = document.createElement("pre");
        line.innerText = text;
        logElem.appendChild(line);
    }
    video.addEventListener("readystatechange", () => {
        log(`🛈 Video ready state = ${video.readyState}`);
    });
    window.onmousedown = (e) => __awaiter(this, void 0, void 0, function* () {
        try {
            if (video.readyState === 4) {
                yield video.play();
            }
        }
        catch (err) {
            log(`✘ ${err}`);
        }
    });
    video.oncanplay = e => {
        log(`🛈 Video can play`);
        //try {
        //    await video.play();
        //} catch (err) {
        //    log(`Video failed to play: ${err}`);
        //}
    };
    function getSignalingSocketUrl() {
        const scheme = location.protocol === "https:" ? "wss" : "ws";
        const port = location.port ? (":" + location.port) : "";
        const url = scheme + "://" + location.hostname + port + "/signaling";
        return url;
    }
    // https://docs.google.com/document/d/1-ZfikoUtoJa9k-GZG1daN0BU3IjIanQ_JSscHxQesvU/edit#
    const pc = new RTCPeerConnection({ sdpSemantics: 'unified-plan' });
    const ws = new WebSocket(getSignalingSocketUrl());
    ws.binaryType = "arraybuffer";
    function send(action, payload) {
        const msg = JSON.stringify({ action, payload });
        log(`🛈 send ${msg}`);
        ws.send(msg);
    }
    ws.onerror = e => {
        log("✘ Websocket error, retrying in 1 second");
        setTimeout(main, 1000);
    };
    ws.onclose = e => {
        log("✘ Websocket closed, retrying in 1 second");
        setTimeout(main, 1000);
    };
    ws.onopen = e => {
        pc.onicecandidate = e => {
            send("ice", e.candidate);
        };
        pc.oniceconnectionstatechange = e => {
            log(`🛈 ice connection state = ${pc.iceConnectionState}`);
        };
        pc.ontrack = ({ transceiver }) => {
            log(`✔ received track`);
            let track = transceiver.receiver.track;
            video.srcObject = new MediaStream([track]);
            track.onunmute = () => {
                log(`✔ track unmuted`);
            };
            track.onended = () => {
                log(`✘ track ended`);
            };
            track.onmute = () => {
                log(`✘ track muted`);
            };
        };
    };
    ws.onmessage = (e) => __awaiter(this, void 0, void 0, function* () {
        const { action, payload } = JSON.parse(e.data);
        log(`🛈 received ${e.data}`);
        try {
            switch (action) {
                case "ice":
                    {
                        yield pc.addIceCandidate(payload);
                        log(`✔ addIceCandidate`);
                        break;
                    }
                case "sdp":
                    {
                        yield pc.setRemoteDescription(payload);
                        log(`✔ setRemoteDescription`);
                        let { sdp, type } = yield pc.createAnswer({ offerToReceiveVideo: true });
                        log(`✔ createAnswer`);
                        sdp = removeBandwidthRestriction(sdp);
                        yield pc.setLocalDescription({ sdp, type });
                        log(`✔ setLocalDescription`);
                        send("sdp", { sdp, type });
                    }
            }
        }
        catch (err) {
            log(`✘ ${err}`);
        }
    });
}
main();
//# sourceMappingURL=index.js.map