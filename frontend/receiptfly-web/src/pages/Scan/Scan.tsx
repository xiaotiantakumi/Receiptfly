import { Camera, Image as ImageIcon, X, Edit, Loader2 } from 'lucide-react';
import styles from './Scan.module.css';
import { useNavigate } from 'react-router-dom';
import { useReceipts } from '../../context/ReceiptContext';
import { useSettings } from '../../context/SettingsContext';
import { useState, useRef, useEffect } from 'react';
import { uploadMultipleBlobs } from '../../services/blobUploadService';

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:7071/api';

export function Scan() {
  const navigate = useNavigate();
  const { receipts, loading, refreshReceipts } = useReceipts();
  const { settings } = useSettings();

  const [cameraActive, setCameraActive] = useState(false);
  const [capturedImage, setCapturedImage] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // 最近のレシートを取得（最新5件）
  const recentReceipts = receipts
    .sort((a, b) => {
      // 日付文字列をパースしてソート
      try {
        const dateA = new Date(
          a.date.replace(/[年月日]/g, '/').replace(/\s+/g, ' ')
        );
        const dateB = new Date(
          b.date.replace(/[年月日]/g, '/').replace(/\s+/g, ' ')
        );
        return dateB.getTime() - dateA.getTime();
      } catch {
        return 0;
      }
    })
    .slice(0, 5);

  const handleReceiptClick = (receiptId: string) => {
    navigate(`/receipts/${receiptId}`);
  };

  // カメラを開始
  const startCamera = async () => {
    try {
      setError(null);
      // まず既存のストリームを停止
      if (streamRef.current) {
        streamRef.current.getTracks().forEach((track) => track.stop());
      }

      const constraints: MediaStreamConstraints = {
        video: {
          facingMode: 'environment', // バックカメラを優先
          width: { ideal: 1920 },
          height: { ideal: 1080 },
        },
        audio: false,
      };

      const stream = await navigator.mediaDevices.getUserMedia(constraints);
      streamRef.current = stream;

      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        videoRef.current.onloadedmetadata = () => {
          videoRef.current?.play().catch((err) => {
            console.error('Video play error:', err);
            setError('ビデオの再生に失敗しました');
          });
        };
        // ストリームが設定されたらカメラをアクティブにする
        setCameraActive(true);
      } else {
        // videoRefがまだ設定されていない場合、少し待ってから再試行
        setTimeout(() => {
          if (videoRef.current) {
            videoRef.current.srcObject = stream;
            videoRef.current.onloadedmetadata = () => {
              videoRef.current?.play().catch((err) => {
                console.error('Video play error:', err);
                setError('ビデオの再生に失敗しました');
              });
            };
            setCameraActive(true);
          }
        }, 100);
      }
    } catch (err) {
      console.error('カメラへのアクセスに失敗しました:', err);
      setError('カメラへのアクセスに失敗しました。権限を確認してください。');
      setCameraActive(false);
    }
  };

  // カメラを停止
  const stopCamera = () => {
    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
    }
    if (videoRef.current) {
      videoRef.current.srcObject = null;
    }
    setCameraActive(false);
    setCapturedImage(null);
  };

  // 画像をキャプチャ
  const captureImage = () => {
    if (videoRef.current && canvasRef.current) {
      const canvas = canvasRef.current;
      const video = videoRef.current;

      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;

      const ctx = canvas.getContext('2d');
      if (ctx) {
        ctx.drawImage(video, 0, 0);
        const imageDataUrl = canvas.toDataURL('image/jpeg');
        setCapturedImage(imageDataUrl);
        stopCamera();
      }
    }
  };

  // ファイル選択
  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      // PDFファイルの場合はプレビューを表示せず、直接アップロード処理に進む
      if (
        file.type === 'application/pdf' ||
        file.name.toLowerCase().endsWith('.pdf')
      ) {
        processPdfFile(file);
        return;
      }

      const reader = new FileReader();
      reader.onload = (e) => {
        setCapturedImage(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  // PDFファイルの処理
  const processPdfFile = async (file: File) => {
    setIsProcessing(true);
    setError(null);

    try {
      // Blob Storageにアップロード
      const uploadResults = await uploadMultipleBlobs([file], 'receipt-images');

      if (!uploadResults[0]?.success || !uploadResults[0]?.blobPath) {
        throw new Error('PDFファイルのアップロードに失敗しました');
      }

      // OCR処理をキューに追加（AccountTitlesとCategoriesを含める）
      const ocrResponse = await fetch(`${API_BASE_URL}/queue-ocr`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          blobPaths: [uploadResults[0].blobPath],
          accountTitles: settings.accountTitles.map((t) => t.name),
          categories: settings.categories.map((c) => c.name),
        }),
      });

      if (!ocrResponse.ok) {
        throw new Error('OCR処理の開始に失敗しました');
      }

      // OCR処理は非同期で実行されるため、キューに追加したことをユーザーに通知
      alert(
        'レシートの処理を開始しました。処理が完了するとレシート一覧に表示されます。'
      );

      // レシート一覧を更新
      await refreshReceipts();

      // ホームに戻る
      navigate('/');
    } catch (err) {
      console.error('PDF処理エラー:', err);
      setError(
        err instanceof Error ? err.message : 'PDF処理中にエラーが発生しました'
      );
    } finally {
      setIsProcessing(false);
    }
  };

  // OCR処理とレシート作成
  const processImage = async () => {
    if (!capturedImage) return;

    setIsProcessing(true);
    setError(null);

    try {
      // Data URLをBlobに変換
      const response = await fetch(capturedImage);
      const blob = await response.blob();
      // ファイル名とMIMEタイプを適切に設定
      const fileName = 'receipt.jpg';
      const fileType = blob.type || 'image/jpeg';
      const file = new File([blob], fileName, { type: fileType });

      // Blob Storageにアップロード
      const uploadResults = await uploadMultipleBlobs([file], 'receipt-images');

      if (!uploadResults[0]?.success || !uploadResults[0]?.blobPath) {
        throw new Error('画像のアップロードに失敗しました');
      }

      // OCR処理をキューに追加（AccountTitlesとCategoriesを含める）
      const ocrResponse = await fetch(`${API_BASE_URL}/queue-ocr`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          blobPaths: [uploadResults[0].blobPath],
          accountTitles: settings.accountTitles.map((t) => t.name),
          categories: settings.categories.map((c) => c.name),
        }),
      });

      if (!ocrResponse.ok) {
        throw new Error('OCR処理の開始に失敗しました');
      }

      // OCR処理は非同期で実行されるため、キューに追加したことをユーザーに通知
      // 実際の実装では、ポーリングやWebSocketを使用して処理完了を待機することを推奨
      // ここでは簡易的に、キューに追加したことを通知してホームに戻る
      alert(
        'レシートの処理を開始しました。処理が完了するとレシート一覧に表示されます。'
      );

      // レシート一覧を更新
      await refreshReceipts();

      // 撮影した画像をクリア
      setCapturedImage(null);

      // ホームに戻る
      navigate('/');
    } catch (err) {
      console.error('画像処理エラー:', err);
      setError(
        err instanceof Error ? err.message : '画像処理中にエラーが発生しました'
      );
    } finally {
      setIsProcessing(false);
    }
  };

  // コンポーネントのアンマウント時にカメラを停止
  useEffect(() => {
    return () => {
      stopCamera();
    };
  }, []);

  return (
    <div className={`${styles.container} animate-fade-in`}>
      <div className={styles.cameraView}>
        {capturedImage ? (
          // キャプチャした画像のプレビュー
          <div className={styles.imagePreviewContainer}>
            <img
              src={capturedImage}
              alt="Captured receipt"
              className={styles.previewImage}
            />
            <div className={styles.previewControls}>
              <button
                className={styles.cancelButton}
                onClick={() => setCapturedImage(null)}
                disabled={isProcessing}
              >
                キャンセル
              </button>
              <button
                className={styles.processButton}
                onClick={processImage}
                disabled={isProcessing}
              >
                {isProcessing ? (
                  <>
                    <Loader2 className={styles.spinner} size={18} />
                    処理中...
                  </>
                ) : (
                  '処理する'
                )}
              </button>
            </div>
          </div>
        ) : cameraActive ? (
          // カメラプレビュー
          <>
            <video
              ref={videoRef}
              className={styles.video}
              autoPlay
              playsInline
              muted
              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
            />
            {error && (
              <div className={styles.errorOverlay}>
                <p className={styles.errorText}>{error}</p>
              </div>
            )}
          </>
        ) : (
          // カメラプレースホルダー
          <div className={styles.cameraPlaceholder}>
            <Camera size={48} className={styles.placeholderIcon} />
            <p>カメラプレビュー</p>
            {error && <p className={styles.errorText}>{error}</p>}
          </div>
        )}

        <canvas ref={canvasRef} style={{ display: 'none' }} />

        <div className={styles.overlay}>
          <div className={styles.header}>
            <button
              className={styles.closeButton}
              onClick={() => {
                stopCamera();
                navigate('/');
              }}
            >
              <X size={24} />
            </button>
            <span className={styles.modeBadge}>レシートモード</span>
          </div>

          {!capturedImage && (
            <div className={styles.controls}>
              <button
                className={styles.galleryButton}
                onClick={() => fileInputRef.current?.click()}
                disabled={isProcessing}
              >
                <ImageIcon size={24} />
              </button>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*,application/pdf"
                style={{ display: 'none' }}
                onChange={handleFileSelect}
              />

              {cameraActive ? (
                <button
                  className={styles.shutterButton}
                  onClick={captureImage}
                  disabled={isProcessing}
                >
                  <div className={styles.shutterInner} />
                </button>
              ) : (
                <button
                  className={styles.shutterButton}
                  onClick={startCamera}
                  disabled={isProcessing}
                >
                  <Camera size={32} />
                </button>
              )}

              <div className={styles.spacer} />
            </div>
          )}
        </div>
      </div>

      <div className={styles.recentScans}>
        <div className={styles.recentHeader}>
          <h3 className={styles.recentTitle}>最近のスキャン</h3>
          <button
            className={styles.manualEntryButton}
            onClick={() => navigate('/manual-entry')}
          >
            <Edit size={18} />
            手動入力
          </button>
        </div>
        <div className={styles.scansList}>
          {loading ? (
            <div className={styles.emptyState}>読み込み中...</div>
          ) : recentReceipts.length === 0 ? (
            <div className={styles.emptyState}>
              まだスキャンされたレシートはありません
            </div>
          ) : (
            <div className={styles.imageGrid}>
              {recentReceipts.map((receipt) => (
                <div
                  key={receipt.id}
                  className={styles.receiptCard}
                  onClick={() => handleReceiptClick(receipt.id)}
                >
                  <div className={styles.receiptThumbnail}>
                    <ImageIcon size={24} className={styles.thumbnailIcon} />
                  </div>
                  <div className={styles.receiptInfo}>
                    <div className={styles.receiptStore}>{receipt.store}</div>
                    <div className={styles.receiptAmount}>
                      ¥{receipt.total.toLocaleString()}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
