'use strict';

const video = document.querySelector('video');
const logElem = document.getElementById('log');

function log(text: string) {
    console.log(text);

    const line = document.createElement("pre");
    line.innerText = text;
    logElem.appendChild(line);
}

video.addEventListener("readystatechange", () => {
    log(`Video ready state = ${video.readyState}`);
});

video.oncanplay = async e => {
    log(`Video can play`);

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

const pc = new RTCPeerConnection();

const ws = new WebSocket(getSignalingSocketUrl());
ws.binaryType = "arraybuffer";

function send(action: "ice" | "sdp", payload: any) {
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
    }
}

ws.onmessage = async e => {
    const { action, payload } = JSON.parse(e.data);
    log(`receive: ${e.data}`);

    try {
        switch (action) {
            case "ice": {
                await pc.addIceCandidate(payload);
                log(`✔: addIceCandidate`);
                break;
            }

            case "sdp": {
                await pc.setRemoteDescription(payload);
                log(`✔: setRemoteDescription`);
                const sdp = await pc.createAnswer({ offerToReceiveVideo: true });
                log(`✔: createAnswer`);
                await pc.setLocalDescription(sdp);
                log(`✔: setLocalDescription`);
                send("sdp", sdp);
            }
        }
    } catch (err) {
        log(err);
    }
}

