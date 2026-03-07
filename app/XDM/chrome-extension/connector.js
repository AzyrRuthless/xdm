"use strict";
import Logger from './logger.js';

const APP_BASE_URL = "http://127.0.0.1:8597";

export default class Connector {
    constructor(onMessage, onDisconnect) {
        this.logger = new Logger();
        this.onMessage = onMessage;
        this.onDisconnect = onDisconnect;
        this.connected = undefined;
    }

    connect() {
        // Clear any old alarms
        chrome.alarms.clearAll();
        // Stagger alarms to keep the service worker alive and checking the connection
        for (let i = 0; i < 12; i++) {
            chrome.alarms.create("alerm-" + i, {
                periodInMinutes: 1,
                when: Date.now() + 1000 + ((i + 1) * 5000)
            });
        }
        chrome.alarms.onAlarm.addListener(this.onTimer.bind(this));

        // Connect natively
        this.nativePort = chrome.runtime.connectNative("org.xdm.native");
        this.nativePort.onMessage.addListener(msg => {
            this.connected = true;
            this.onMessage(msg);
        });
        this.nativePort.onDisconnect.addListener(() => {
            this.logger.log("Native port disconnected. Retrying natively via alarms...");
            this.disconnect();
            this.nativePort = null;
        });
    }

    onTimer() {
        // Re-establish native port if dead
        if (!this.nativePort) {
            this.nativePort = chrome.runtime.connectNative("org.xdm.native");
            this.nativePort.onMessage.addListener(msg => {
                this.connected = true;
                this.onMessage(msg);
            });
            this.nativePort.onDisconnect.addListener(() => {
                this.nativePort = null;
            });
        }

        // Fallback or secondary sync
        fetch(APP_BASE_URL + "/sync")
            .then(this.onResponse.bind(this))
            .catch(err => {
                if (!this.nativePort) this.disconnect();
            });
    }

    disconnect() {
        this.connected = false;
        this.onDisconnect();
    }

    isConnected() {
        return this.connected;
    }

    onResponse(res) {
        this.connected = true;
        res.json().then(json => this.onMessage(json)).catch(err => this.disconnect());
    }

    postMessage(url, data) {
        if (this.nativePort) {
            try {
                this.nativePort.postMessage({ url: url, data: data });
            } catch (err) {
                this.logger.log("Native post error: " + err);
            }
        }

        fetch(APP_BASE_URL + url, { method: "POST", body: JSON.stringify(data) })
            .then(this.onResponse.bind(this))
            .catch(err => {
                 if (!this.nativePort) this.disconnect();
            });
    }

    launchApp() {

    }
}