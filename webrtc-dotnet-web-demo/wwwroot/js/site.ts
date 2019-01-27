'use strict';

const video = document.querySelector('video');

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
    const msg = { action, payload };
    console.log("send", msg);
    ws.send(JSON.stringify(msg));
}

ws.onopen = e => {
    pc.onicecandidate = e => {
        send("ice", e.candidate);
    };

    pc.oniceconnectionstatechange = e => {
        console.info(`ice connection state = ${pc.iceConnectionState}`);
    };

    pc.ontrack = e => {
        const stream = e.streams[0];
        if (video.srcObject !== stream) {
            video.srcObject = stream;
            console.info(`✔: received media stream`);
        }
    }
}

ws.onmessage = async e => {
    const { action, payload } = JSON.parse(e.data);
    console.log("msg", { action, payload });

    try {
        switch (action) {
            case "ice": {
                await pc.addIceCandidate(payload);
                console.info(`✔: addIceCandidate`);
                break;
            }

            case "sdp": {
                await pc.setRemoteDescription(payload);
                console.info(`✔: setRemoteDescription`);
                const sdp = await pc.createAnswer({ offerToReceiveVideo: true });
                console.info(`✔: createAnswer`);
                await pc.setLocalDescription(sdp);
                console.info(`✔: setLocalDescription`);
                send("sdp", sdp);
            }
        }
    } catch (err) {
        console.error(err);
    }
}

