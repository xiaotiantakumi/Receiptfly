import { useState, useRef, useEffect } from 'react';
import { Camera, Image as ImageIcon, X, Edit, Upload, Trash2, Loader2 } from 'lucide-react';
import styles from './Scan.module.css';
import { useNavigate } from 'react-router-dom';

interface CapturedImage {
  id: string;
  file: File;
  preview: string;
}

interface OcrResult {
  fileName: string;
  text?: string;
  filePath?: string;
  error?: string;
}

export function Scan() {
  const navigate = useNavigate();
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [capturedImages, setCapturedImages] = useState<CapturedImage[]>([]);
  const [isCameraActive, setIsCameraActive] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [ocrResults, setOcrResults] = useState<OcrResult[]>([]);

  // カメラの初期化
  useEffect(() => {
    startCamera();
    return () => {
      stopCamera();
    };
  }, []);

  const startCamera = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' } // バックカメラを優先
      });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        setIsCameraActive(true);
      }
    } catch (error) {
      console.error('カメラの起動に失敗しました:', error);
      setIsCameraActive(false);
    }
  };

  const stopCamera = () => {
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => track.stop());
      streamRef.current = null;
    }
    if (videoRef.current) {
      videoRef.current.srcObject = null;
    }
    setIsCameraActive(false);
  };

  const captureImage = () => {
    if (!videoRef.current) return;

    const canvas = document.createElement('canvas');
    canvas.width = videoRef.current.videoWidth;
    canvas.height = videoRef.current.videoHeight;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.drawImage(videoRef.current, 0, 0);
    canvas.toBlob((blob) => {
      if (blob) {
        const file = new File([blob], `receipt-${Date.now()}.png`, { type: 'image/png' });
        const preview = URL.createObjectURL(blob);
        const newImage: CapturedImage = {
          id: Date.now().toString(),
          file,
          preview
        };
        setCapturedImages(prev => [...prev, newImage]);
      }
    }, 'image/png');
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files) return;

    const newImages: CapturedImage[] = [];
    Array.from(files).forEach((file) => {
      if (file.type.startsWith('image/')) {
        const preview = URL.createObjectURL(file);
        newImages.push({
          id: Date.now().toString() + Math.random(),
          file,
          preview
        });
      }
    });
    setCapturedImages(prev => [...prev, ...newImages]);
    
    // ファイル入力のリセット
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const removeImage = (id: string) => {
    setCapturedImages(prev => {
      const image = prev.find(img => img.id === id);
      if (image) {
        URL.revokeObjectURL(image.preview);
      }
      return prev.filter(img => img.id !== id);
    });
  };

  const handleBatchUpload = async () => {
    if (capturedImages.length === 0) return;

    setIsUploading(true);
    setOcrResults([]);

    try {
      const formData = new FormData();
      capturedImages.forEach((image) => {
        formData.append('files', image.file);
      });

      const response = await fetch('http://localhost:5159/api/ocr/batch', {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        throw new Error('OCR処理に失敗しました');
      }

      const data = await response.json();
      setOcrResults(data.results || []);
      
      // OCR結果をコンソールに出力（将来的にLLM処理に渡す）
      console.log('OCR結果:', data.results);
    } catch (error) {
      console.error('アップロードエラー:', error);
      alert('画像のアップロードに失敗しました');
    } finally {
      setIsUploading(false);
    }
  };

  const handleClose = () => {
    stopCamera();
    navigate('/');
  };

  return (
    <div className={`${styles.container} animate-fade-in`}>
      <div className={styles.cameraView}>
        {isCameraActive ? (
          <video
            ref={videoRef}
            autoPlay
            playsInline
            className={styles.video}
          />
        ) : (
          <div className={styles.cameraPlaceholder}>
            <Camera size={48} className={styles.placeholderIcon} />
            <p>カメラを起動できませんでした</p>
          </div>
        )}
        
        <div className={styles.overlay}>
          <div className={styles.header}>
            <button className={styles.closeButton} onClick={handleClose}>
              <X size={24} />
            </button>
            <span className={styles.modeBadge}>レシートモード</span>
          </div>

          <div className={styles.controls}>
            <button 
              className={styles.galleryButton}
              onClick={() => fileInputRef.current?.click()}
            >
              <ImageIcon size={24} />
            </button>
            
            <button 
              className={styles.shutterButton}
              onClick={captureImage}
              disabled={!isCameraActive}
            >
              <div className={styles.shutterInner} />
            </button>
            
            <div className={styles.spacer} /> 
          </div>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          accept="image/*"
          multiple
          onChange={handleFileSelect}
          style={{ display: 'none' }}
        />
      </div>
      
      <div className={styles.recentScans}>
        <div className={styles.recentHeader}>
          <h3 className={styles.recentTitle}>
            {capturedImages.length > 0 ? `選択中の画像 (${capturedImages.length})` : '最近のスキャン'}
          </h3>
          <button 
            className={styles.manualEntryButton}
            onClick={() => navigate('/manual-entry')}
          >
            <Edit size={18} />
            手動入力
          </button>
        </div>

        {capturedImages.length > 0 ? (
          <div className={styles.imagePreviewContainer}>
            <div className={styles.imageGrid}>
              {capturedImages.map((image) => (
                <div key={image.id} className={styles.imagePreview}>
                  <img src={image.preview} alt="Preview" />
                  <button
                    className={styles.removeButton}
                    onClick={() => removeImage(image.id)}
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              ))}
            </div>
            <button
              className={styles.uploadButton}
              onClick={handleBatchUpload}
              disabled={isUploading}
            >
              {isUploading ? (
                <>
                  <Loader2 size={18} className={styles.spinner} />
                  処理中...
                </>
              ) : (
                <>
                  <Upload size={18} />
                  一括アップロード ({capturedImages.length}枚)
                </>
              )}
            </button>
            {ocrResults.length > 0 && (
              <div className={styles.ocrResults}>
                <h4>OCR結果</h4>
                {ocrResults.map((result, index) => (
                  <div key={index} className={styles.ocrResultItem}>
                    <strong>{result.fileName}</strong>
                    {result.text ? (
                      <pre className={styles.ocrText}>{result.text}</pre>
                    ) : (
                      <p className={styles.ocrError}>{result.error}</p>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : (
          <div className={styles.emptyState}>
            まだスキャンされたレシートはありません
          </div>
        )}
      </div>
    </div>
  );
}
