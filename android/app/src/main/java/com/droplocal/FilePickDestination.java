package com.droplocal;

import java.util.List;

// Owned by one file-picker operation, independently of the visible discovery list.
final class FilePickDestination {
    private LocalDiscovery.Device device;
    final PairSession.Peer peer;

    FilePickDestination(LocalDiscovery.Device device, PairSession.Peer peer) {
        this.device = device;
        this.peer = device == null ? peer : null;
    }

    void refresh(List<LocalDiscovery.Device> devices) {
        if (device == null) return;
        for (LocalDiscovery.Device latest : devices) {
            if (latest.id.equals(device.id)) {
                device = latest;
                return;
            }
        }
        // Missing multicast advertisements do not prove that TCP is unavailable.
    }

    LocalDiscovery.Device device() { return device; }
}
