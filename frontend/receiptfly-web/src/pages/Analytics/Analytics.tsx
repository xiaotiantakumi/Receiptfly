import React from 'react';
import { useReceipts } from '../../context/ReceiptContext';
import { PieChart } from 'lucide-react';
import styles from './Analytics.module.css';

const Analytics: React.FC = () => {
  const { receipts } = useReceipts();

  // Aggregate data by category
  const categoryTotals = receipts.reduce((acc, receipt) => {
    receipt.items.forEach(item => {
      const category = item.category || '未分類';
      acc[category] = (acc[category] || 0) + item.amount;
    });
    return acc;
  }, {} as Record<string, number>);

  const sortedCategories = Object.entries(categoryTotals)
    .sort(([, a], [, b]) => b - a);

  const totalSpending = Object.values(categoryTotals).reduce((a, b) => a + b, 0);

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <h1><PieChart className={styles.icon} /> 集計レポート</h1>
        <div className={styles.totalCard}>
          <span className={styles.totalLabel}>総支出</span>
          <span className={styles.totalAmount}>¥{totalSpending.toLocaleString()}</span>
        </div>
      </header>

      <div className={styles.chartSection}>
        <h2>カテゴリ別支出</h2>
        <div className={styles.categoryList}>
          {sortedCategories.map(([category, amount]) => {
            const percentage = Math.round((amount / totalSpending) * 100);
            return (
              <div key={category} className={styles.categoryItem}>
                <div className={styles.categoryInfo}>
                  <span className={styles.categoryName}>{category}</span>
                  <span className={styles.categoryAmount}>¥{amount.toLocaleString()}</span>
                </div>
                <div className={styles.progressBarContainer}>
                  <div 
                    className={styles.progressBar} 
                    style={{ width: `${percentage}%` }}
                  />
                </div>
                <span className={styles.percentage}>{percentage}%</span>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default Analytics;
