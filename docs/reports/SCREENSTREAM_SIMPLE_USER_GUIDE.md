# LAF ScreenStream Assist — Simple User Guide

This guide is written for a non-coder (think: a 15-year-old). Follow the steps in order.

## Before you start

- You need **Windows 11** with **.NET 10** installed.
- Everyone who shares their screen does so **on purpose** — the client app is always visible
  and only shares the **main screen**. There is no hidden mode and no remote control.

## Steps

1. Open the folder `C:\LAFScreenStreamAssist\Server`.
2. **Double-click `Start-Server.bat`.** A window opens and the server starts.
3. Open your web browser and go to **`http://localhost:5090`**. You will see the dashboard.
4. Click the **Generate Client** button. This creates a Client folder under
   `C:\LAFScreenStreamAssist\GeneratedClients\`.
5. **Send that whole Client folder** to the person whose screen you want to see (zip it and
   send it, or copy it to their PC).
6. The other person opens the folder and **double-clicks `LAFScreenStream.Client.exe`.**
7. A small **visible window** appears on their screen that says their **primary screen is
   being shared**, with a **Disconnect** button. Their main screen now shows on your dashboard.
8. When done, they (or you) click **Disconnect**. Sharing stops.

## Where it works

- **Same computer** (loopback): works.
- **Same home/office network** (LAN): works — you may need to allow it through **Windows
  Firewall** the first time.
- **Over the internet**: this sample is **not** internet-ready out of the box. It needs a
  reachable public address (port-forward or relay) **plus** secure TLS. That is extra network
  setup done by whoever runs the server. See the Network Feasibility report.

## Safety, in plain words

The client only shares the main screen. It does **not** read your keyboard, clipboard, files,
webcam, or microphone, and it **cannot control** the other PC.
