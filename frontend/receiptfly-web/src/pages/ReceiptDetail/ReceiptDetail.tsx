import { ArrowLeft, Calendar, Store } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import styles from './ReceiptDetail.module.css';
import { useReceipts } from '../../context/ReceiptContext';

export function ReceiptDetail() {
  const navigate = useNavigate();
  const { id } = useParams();
  const { receipts, updateItem } = useReceipts();
  
  const receipt = receipts.find(r => r.id === Number(id));

  if (!receipt) {
    return <div className={styles.container}>Receipt not found</div>;
  }

  const toggleTaxReturn = (itemId: number, currentStatus: boolean) => {
    updateItem(receipt.id, itemId, { isTaxReturn: !currentStatus });
  };

  const taxReturnTotal = receipt.items
    .filter(item => item.isTaxReturn)
    .reduce((sum, item) => sum + item.amount, 0);

  return (
    <div className={`${styles.container} animate-slide-in`}>
      <header className={styles.header}>
        <button onClick={() => navigate(-1)} className={styles.backButton}>
          <ArrowLeft size={24} />
        </button>
        <h1 className={styles.title}>レシート詳細</h1>
        <div className={styles.placeholder} />
      </header>

      <div className={styles.receiptCard}>
        <div className={styles.cardHeader}>
          <div className={styles.storeInfo}>
            <Store size={20} className={styles.icon} />
            <span className={styles.storeName}>{receipt.store}</span>
          </div>
          <div className={styles.dateInfo}>
            <Calendar size={16} className={styles.icon} />
            <span>{receipt.date}</span>
          </div>
        </div>
        
        <div className={styles.totalRow}>
          <div className={styles.totalBlock}>
            <span className={styles.totalLabel}>支払総額</span>
            <span className={styles.totalAmount}>¥{receipt.total.toLocaleString()}</span>
          </div>
          <div className={styles.totalBlockRight}>
            <span className={styles.totalLabel}>申告対象</span>
            <span className={styles.taxTotalAmount}>¥{taxReturnTotal.toLocaleString()}</span>
          </div>
        </div>
      </div>

      <section className={styles.itemsSection}>
        <h2 className={styles.sectionTitle}>購入品目</h2>
        <div className={styles.itemsList}>
          {receipt.items.map((item) => (
            <div key={item.id} className={styles.itemRow} onClick={() => toggleTaxReturn(item.id, item.isTaxReturn)}>
              <div className={`${styles.checkboxWrapper} ${item.isTaxReturn ? styles.checked : ''}`}>
                {item.isTaxReturn && <div className={styles.checkIndicator} />}
              </div>
              
              <div className={styles.itemInfo}>
                <span className={styles.itemName}>{item.name}</span>
              </div>
              
              <div className={styles.amountColumn}>
                <span className={styles.itemAmount}>¥{item.amount}</span>
                {item.isTaxReturn && <span className={styles.taxBadge}>申告用</span>}
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
