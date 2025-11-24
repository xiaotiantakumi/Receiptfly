import { Camera, Image as ImageIcon, X, Edit } from 'lucide-react';
import styles from './Scan.module.css';
import { useNavigate } from 'react-router-dom';
import { useReceipts } from '../../context/ReceiptContext';

export function Scan() {
  const navigate = useNavigate();
  const { receipts, loading } = useReceipts();

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

  return (
    <div className={`${styles.container} animate-fade-in`}>
      <div className={styles.cameraView}>
        {/* Placeholder for Camera Stream */}
        <div className={styles.cameraPlaceholder}>
          <Camera size={48} className={styles.placeholderIcon} />
          <p>カメラプレビュー</p>
        </div>

        <div className={styles.overlay}>
          <div className={styles.header}>
            <button
              className={styles.closeButton}
              onClick={() => navigate('/')}
            >
              <X size={24} />
            </button>
            <span className={styles.modeBadge}>レシートモード</span>
          </div>

          <div className={styles.controls}>
            <button className={styles.galleryButton}>
              <ImageIcon size={24} />
            </button>

            <button className={styles.shutterButton}>
              <div className={styles.shutterInner} />
            </button>

            <div className={styles.spacer} />
          </div>
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
