'use strict';

let retryHandle: number = NaN;

function isPlaying(media: HTMLMediaElement): boolean {
    return media.currentTime > 0 && !media.paused && !media.ended && media.readyState > 2;
}

// https://github.com/webrtc/samples/blob/gh-pages/src/content/peerconnection/bandwidth/js/main.js
function removeBandwidthRestriction(sdp: string) {
    // TODO: This this is actually work? Test!
    return sdp.replace(/b=AS:.*\r\n/, '').replace(/b=TIAS:.*\r\n/, '');
}

function main() {

    retryHandle = NaN;

    const video = document.querySelector('video');
    const logElem = document.getElementById('log');
    const playElem = document.getElementById('play-trigger');
    playElem.style.visibility = "hidden";

    // Clear log
    logElem.innerText = "";

    function log(text: string) {

        console.log(text);

        const line = document.createElement("pre");
        line.innerText = text;
        logElem.appendChild(line);
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

    function send(action: "ice" | "sdp" | "pos", payload: any) {
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

    video.addEventListener("readystatechange", () => log(`🛈 Video ready state = ${video.readyState}`));

    function sendMousePos(e: MouseEvent, kind: number) {
        const bounds = video.getBoundingClientRect();
        const x = (e.clientX - bounds.left) / bounds.width;
        const y = (e.clientY - bounds.top) / bounds.height;
        send("pos", { kind, x, y });

        if (kind === 2) {
            video.onmousemove = video.onmouseup = null;
        }
    }

    video.onmousedown = async (e: MouseEvent) => {
        if (e.button === 0) {
            sendMousePos(e, 0);
            video.onmousemove = (e2: MouseEvent) => sendMousePos(e2, 1);
            video.onmouseup = (e2: MouseEvent) => sendMousePos(e2, 2);
        }
    }

    playElem.onmousedown = async (e: MouseEvent) => {
        try {
            if (e.button === 0) {
                log(`🛈 Playing video`);
                await video.play();
                playElem.style.visibility = "hidden";
            }
        } catch (err) {
            log(`✘ ${err}`);
        }
    }

    video.oncanplay = () => {
        log(`🛈 Video can play`);
        playElem.style.visibility = "visible";
    };

    ws.onerror = () => log(`✘ websocket error`);
    ws.onclose = () => retry("websocket closed");

    ws.onopen = () => {
        pc.onicecandidate = e => {
            send("ice", e.candidate);
        };

        pc.oniceconnectionstatechange = e => {
            log(`🛈 ice connection state = ${pc && pc.iceConnectionState}`);
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

            track.onmute = () => {
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
