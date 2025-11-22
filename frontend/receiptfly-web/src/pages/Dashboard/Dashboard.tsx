import { TrendingUp } from 'lucide-react';
import styles from './Dashboard.module.css';

import { useNavigate } from 'react-router-dom';
import { useReceipts } from '../../context/ReceiptContext';

export function Dashboard() {
  const navigate = useNavigate();
  const { receipts, loading } = useReceipts();

  // Calculate monthly totals
  const monthlyTotal = receipts.length > 0 ? receipts.reduce((sum, r) => sum + r.total, 0) : 0;
  const monthlyTaxReturnTotal = receipts.length > 0 ? receipts.reduce((sum, r) => {
    return sum + r.items.filter(i => i.isTaxReturn).reduce((subSum, i) => subSum + i.amount, 0);
  }, 0) : 0;

  return (
    <div className={`${styles.container} animate-fade-in`}>
      <header className={styles.header}>
        <div>
          <h1 className={styles.title}>概要</h1>
          <p className={styles.subtitle}>おかえりなさい、Takumiさん</p>
        </div>
        <div className={styles.avatar}>T</div>
      </header>

      <section className={styles.summarySection}>
        <div className={styles.mainCard}>
          <div className={styles.cardRow}>
            <div>
              <span className={styles.cardLabel}>今月の支出</span>
              <div className={styles.amountWrapper}>
                <span className={styles.currency}>¥</span>
                <span className={styles.amount}>{monthlyTotal.toLocaleString()}</span>
              </div>
            </div>
            <div className={styles.taxTotalBlock}>
              <span className={styles.cardLabel}>申告対象</span>
              <div className={styles.amountWrapperSmall}>
                <span className={styles.currencySmall}>¥</span>
                <span className={styles.amountSmall}>{monthlyTaxReturnTotal.toLocaleString()}</span>
              </div>
            </div>
          </div>
          <div className={styles.trend}>
            <TrendingUp size={16} className={styles.trendIcon} />
            <span>先月比 +12%</span>
          </div>
        </div>


      </section>

      <section className={styles.recentSection}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>最近のレシート</h2>
        </div>
        
        <div className={styles.transactionList}>
          {loading ? (
            <div>読み込み中...</div>
          ) : receipts.length === 0 ? (
            <div>レシートがありません</div>
          ) : (
            receipts.map((receipt) => {
              const taxReturnAmount = receipt.items
                .filter(item => item.isTaxReturn)
                .reduce((sum, item) => sum + item.amount, 0);

              return (
                <div 
                  key={receipt.id} 
                  className={styles.transactionItem}
                  onClick={() => navigate(`/receipts/${receipt.id}`)}
                >
                  <div className={styles.transactionIcon}>🧾</div>
                  
                  <div className={styles.transactionInfo}>
                    <span className={styles.itemName}>{receipt.store}</span>
                    <span className={styles.itemMeta}>{receipt.date} • {receipt.items.length}点</span>
                  </div>
                  
                  <div className={styles.amountColumn}>
                    <span className={styles.transactionAmount}>¥{receipt.total.toLocaleString()}</span>
                    {taxReturnAmount > 0 && (
                      <span className={styles.taxReturnAmount}>
                        (申告: ¥{taxReturnAmount.toLocaleString()})
                      </span>
                    )}
                  </div>
                </div>
              );
            })
          )}
        </div>
      </section>
    </div>
  );
}
