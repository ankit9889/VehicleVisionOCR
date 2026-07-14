import { create } from 'zustand';

interface OcrField {
  key: string;
  value: string;
  confidence: { percentage: number; isReliable: boolean };
}

interface DetectedText {
  text: string;
  confidence: { percentage: number; isReliable: boolean };
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface OcrResultData {
  rawText: string;
  detectedTexts: DetectedText[];
  extractedFields: OcrField[];
  overallConfidence: { percentage: number; isReliable: boolean };
}

interface OcrState {
  isProcessing: boolean;
  lastResult: OcrResultData | null;
  lastImage: string | null; // url or base64
  setProcessing: (status: boolean) => void;
  setResult: (result: OcrResultData | null) => void;
  setLastImage: (image: string | null) => void;
}

export const useOcrStore = create<OcrState>((set) => ({
  isProcessing: false,
  lastResult: null,
  lastImage: null,
  setProcessing: (status) => set({ isProcessing: status }),
  setResult: (result) => set({ lastResult: result }),
  setLastImage: (image) => set({ lastImage: image }),
}));
