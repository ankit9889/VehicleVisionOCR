import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface ScannerInfo {
  id: string;
  name: string;
  serialNumber: string;
  firmwareVersion: string;
  brand: number;
  type: number;
}

interface ScannerState {
  connectedScanner: ScannerInfo | null;
  status: string;
  setConnectedScanner: (scanner: ScannerInfo | null) => void;
  setStatus: (status: string) => void;
}

export const useScannerStore = create<ScannerState>()(
  persist(
    (set) => ({
      connectedScanner: null,
      status: 'Disconnected',
      setConnectedScanner: (scanner) => set({ connectedScanner: scanner }),
      setStatus: (status) => set({ status }),
    }),
    {
      name: 'scanner-storage',
    }
  )
);
