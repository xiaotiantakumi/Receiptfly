import { Camera, Image as ImageIcon, X } from 'lucide-react';
import styles from './Scan.module.css';
import { useState } from 'react';

export function Scan() {
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
            <button className={styles.closeButton}>
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
        <h3 className={styles.recentTitle}>最近のスキャン</h3>
        <div className={styles.scansList}>
          <div className={styles.emptyState}>
            まだスキャンされたレシートはありません
          </div>
        </div>
      </div>
    </div>
  );
}
