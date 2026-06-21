# LAF ScreenStream Assist — Simple Test Guide

**Status:** LAN_READY (not production-grade)
**Date:** 2026-06-21

This is the plain, step-by-step guide for testing screen sharing between two PCs. No
technical knowledge needed. **Only use this with the other person's consent.**

## The 11 steps

1. On the **host PC**, open the folder `C:\LAFScreenStreamAssist\Server`.
2. **Double-click `Start-Server.bat`.**
3. **Wait** for your web browser to open (it shows the dashboard at
   `http://localhost:5090`). If it doesn't open by itself, type that address into your
   browser.
4. On the dashboard, click **Generate Client** and give it a name (for example,
   `AkshayTestClient`). A new folder appears under
   `C:\LAFScreenStreamAssist\GeneratedClients\<name>\`.
5. **Send that whole generated client folder** to the other PC (USB stick, shared folder,
   or chat — whatever you normally use).
6. On the **other PC**, open the folder you copied and **double-click
   `LAFScreenStream.Client.exe`.**
7. The person on the other PC will see a **window showing that their screen is being
   shared**. (Their screen now streams to your dashboard.)
8. On your **host PC dashboard**, you'll see the client **connected** and a **frame
   counter going up** — that means it's working.
9. The other person can click **Disconnect** at any time (or you can disconnect from the
   dashboard) to **stop sharing immediately.**
10. If both PCs are on the **same Wi-Fi / LAN**, Windows Firewall may pop up the first
    time — choose **Allow** so the two PCs can talk.
11. To use it **over the internet** (not just same Wi-Fi), you need a **fixed public
    address** — that means a **port-forward on your router or a tunnel/relay service**.
    This is not set up for you and must be done by the network owner.

## Reminders

- **Consent first.** Do not run this on anyone's PC without their clear permission. The
  client always shows a visible sharing window.
- This tool only shares the **screen**. It does **not** record your keyboard, clipboard,
  files, webcam, or microphone, and it cannot control the other PC.
- Today this works reliably on **loopback (same PC)** and on a **LAN (same Wi-Fi)** with
  the firewall allowed. Internet use needs the extra network setup in step 11 and is not
  yet production-grade (no TLS/WSS encryption yet).
