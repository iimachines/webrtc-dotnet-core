'use strict';

let retryHandle: number = NaN;

function main() {

    retryHandle = NaN;

    const video = document.querySelector('video');
    const logElem = document.getElementById('log');

    // Clear log
    logElem.innerText = "";

    // https://github.com/webrtc/samples/blob/gh-pages/src/content/peerconnection/bandwidth/js/main.js
    function removeBandwidthRestriction(sdp: string) {
        // TODO: Doesn't seem to work...
        return sdp.replace(/b=AS:.*\r\n/, '').replace(/b=TIAS:.*\r\n/, '');
    }

    function log(text: string) {

        console.log(text);

        const line = document.createElement("pre");
        line.innerText = text;
        logElem.appendChild(line);
    }

    video.addEventListener("readystatechange",
        () => {
            log(`🛈 Video ready state = ${video.readyState}`);
        });

    window.onmousedown = async e =>
    {
        try {
            if (video.readyState === 4) {
                await video.play();
            }
        } catch (err) {
            log(`✘ ${err}`);
        }
    }

    video.oncanplay = e => {
        log(`🛈 Video can play`);
        //try {
        //    await video.play();
        //} catch (err) {
        //    log(`Video failed to play: ${err}`);
        //}
    }

    function getSignalingSocketUrl() {
        const scheme = location.protocol === "https:" ? "wss" : "ws";
        const port = location.port ? (":" + location.port) : "";
        const url = scheme + "://" + location.hostname + port + "/signaling";
        return url;
    }

    // https://docs.google.com/document/d/1-ZfikoUtoJa9k-GZG1daN0BU3IjIanQ_JSscHxQesvU/edit#
    let pc = new RTCPeerConnection({ sdpSemantics: 'unified-plan' } as RTCConfiguration);

    let ws = new WebSocket(getSignalingSocketUrl());
    ws.binaryType = "arraybuffer";

    function send(action: "ice" | "sdp", payload: any) {
        const msg = JSON.stringify({ action, payload });
        log(`🛈 send ${msg}`);
        ws.send(msg);
    }

    function retry(reason: string) {
        clearTimeout(retryHandle);
        retryHandle = NaN;

        log(`✘ retrying in 1 second: ${reason}`);

        ws.close();
        pc.close();

        pc = null;
        ws = null;

        setTimeout(main, 1000);
    }

    ws.onerror = e => log(`✘ websocket error`);
    ws.onclose = e => retry("websocket closed");

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
            }

            track.onended = () => {
                log(`✘ track ended`);
            }

            track.onmute = () =>
            {
                log(`✘ track muted`);
            };
        }
    }

    ws.onmessage = async e => {
        const { action, payload } = JSON.parse(e.data);
        log(`🛈 received ${e.data}`);

        try {
            switch (action) {
            case "ice":
            {
                await pc.addIceCandidate(payload);
                log(`✔ addIceCandidate`);
                break;
            }

            case "sdp":
            {
                await pc.setRemoteDescription(payload);
                log(`✔ setRemoteDescription`);
                let { sdp, type } = await pc.createAnswer({ offerToReceiveVideo: true });
                log(`✔ createAnswer`);
                sdp = removeBandwidthRestriction(sdp);
                await pc.setLocalDescription({ sdp, type });
                log(`✔ setLocalDescription`);
                send("sdp", { sdp, type });
            }
            }
        } catch (err) {
            log(`✘ ${err}`);
        }
    }
}

main();
