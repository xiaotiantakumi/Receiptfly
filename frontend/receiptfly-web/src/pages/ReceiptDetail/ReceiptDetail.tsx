import { ArrowLeft, Store, Edit2, X } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import styles from './ReceiptDetail.module.css';
import { useReceipts } from '../../context/ReceiptContext';
import { useState } from 'react';
import { type TransactionItem } from '../../data/mockData';

export function ReceiptDetail() {
  const navigate = useNavigate();
  const { id } = useParams();
  const { receipts, updateItem, updateReceipt } = useReceipts();
  const [isEditingReceipt, setIsEditingReceipt] = useState(false);
  const [receiptEditForm, setReceiptEditForm] = useState({ store: '', date: '', tel: '' });
  // New state to track which items are expanded for inline editing (supports multiple)
  const [expandedItemIds, setExpandedItemIds] = useState<number[]>([]);
  // State to hold form data for each expanded item, keyed by item ID
  const [editForms, setEditForms] = useState<Record<number, {
    aiCategory: string;
    aiRisk: string;
    memo: string;
    taxType: string;
    accountTitle: string;
    isTaxReturn: boolean;
    amount: number;
  }>>({});
  
  const receipt = receipts.find(r => r.id === Number(id));

  if (!receipt) {
    return <div className={styles.container}>Receipt not found</div>;
  }

  const toggleTaxReturn = (itemId: number, currentStatus: boolean) => {
    updateItem(receipt.id, itemId, { isTaxReturn: !currentStatus });
  };

  const toggleEditItem = (item: TransactionItem) => {
    setExpandedItemIds(prev => {
      if (prev.includes(item.id)) {
        // Close if already open
        const newIds = prev.filter(id => id !== item.id);
        // Clean up form state
        setEditForms(forms => {
          const newForms = { ...forms };
          delete newForms[item.id];
          return newForms;
        });
        return newIds;
      } else {
        // Open if closed
        // Initialize form state for this item
        setEditForms(forms => ({
          ...forms,
          [item.id]: {
            aiCategory: item.aiCategory || '',
            aiRisk: item.aiRisk || 'Low',
            memo: item.memo || '',
            taxType: item.taxType || '10%',
            accountTitle: item.accountTitle || '',
            isTaxReturn: item.isTaxReturn,
            amount: item.amount
          }
        }));
        return [...prev, item.id];
      }
    });
  };

  const handleCancelEdit = (itemId: number) => {
    setExpandedItemIds(prev => prev.filter(id => id !== itemId));
    setEditForms(forms => {
      const newForms = { ...forms };
      delete newForms[itemId];
      return newForms;
    });
  };

  const handleReceiptEditClick = () => {
    if (!receipt) return;
    setReceiptEditForm({
      store: receipt.store,
      date: receipt.date,
      tel: receipt.tel || ''
    });
    setIsEditingReceipt(true);
  };

  const handleSaveReceiptEdit = () => {
    if (receipt) {
      updateReceipt(receipt.id, {
        store: receiptEditForm.store,
        date: receiptEditForm.date,
        tel: receiptEditForm.tel
      });
      setIsEditingReceipt(false);
    }
  };

  const handleSaveEdit = (itemId: number) => {
    const form = editForms[itemId];
    if (form) {
      updateItem(receipt.id, itemId, {
        aiCategory: form.aiCategory,
        aiRisk: form.aiRisk,
        memo: form.memo,
        taxType: form.taxType,
        accountTitle: form.accountTitle,
        isTaxReturn: form.isTaxReturn,
        amount: Number(form.amount)
      });
      handleCancelEdit(itemId); // Close and cleanup
    }
  };

  const taxReturnTotal = receipt.items
    .filter(item => item.isTaxReturn)
    .reduce((sum, item) => sum + item.amount, 0);

  return (
    <div className={`${styles.container} animate-slide-in`}>
      <header className={styles.header}>
        <div className={styles.headerLeft}>
          <button onClick={() => navigate(-1)} className={styles.backButton}>
            <ArrowLeft size={24} />
          </button>
          <div className={styles.storeInfo}>
            <Store size={20} />
            <h1>{receipt.store}</h1>
          </div>
        </div>
        <div className={styles.headerRight}>
          <span className={styles.dateText}>{receipt.date}</span>
          <button onClick={handleReceiptEditClick} className={styles.editReceiptButton}>
            <Edit2 size={18} />
          </button>
        </div>
      </header>

      <div className={styles.receiptCard}>
        {/* Original cardHeader content removed as store and date are now in the main header */}
        
        <div className={styles.metaInfo}>
          {receipt.address && <div className={styles.metaRow}>📍 {receipt.address}</div>}
          {receipt.tel && <div className={styles.metaRow}>📞 {receipt.tel}</div>}
          {receipt.paymentMethod && <div className={styles.metaRow}>💳 {receipt.paymentMethod}</div>}
          {receipt.registrationNumber && <div className={styles.metaRow}>🔢 T番号: {receipt.registrationNumber}</div>}
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
            <div key={item.id}>
              <div className={styles.itemRow} onClick={() => toggleEditItem(item)}>
                {/* Checkbox wrapper removed */}
                
                <div className={styles.itemInfo}>
                  <span className={styles.itemName}>{item.name}</span>
                  <div className={styles.itemMetaTags}>
                    {item.aiCategory && <span className={styles.aiTag}>🤖 {item.aiCategory}</span>}
                    {item.aiRisk && (
                      <span className={`${styles.riskTag} ${styles[item.aiRisk.toLowerCase()]}`}>
                        Risk: {item.aiRisk}
                      </span>
                    )}
                    {item.taxType && <span className={styles.taxTypeTag}>{item.taxType}</span>}
                    {item.accountTitle && <span className={styles.accountTitleTag}>{item.accountTitle}</span>}
                  </div>
                  {item.memo && <div className={styles.itemMemo}>📝 {item.memo}</div>}
                </div>
                
                <div className={styles.actionsColumn}>
                  <button 
                    className={styles.editButton}
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleEditItem(item);
                    }}
                  >
                    <Edit2 size={16} />
                  </button>
                  <div className={styles.amountColumn}>
                    <span className={styles.itemAmount}>¥{item.amount}</span>
                    {item.isTaxReturn && <span className={styles.taxBadge}>申告用</span>}
                  </div>
                </div>
              </div>
              
              {/* Inline Edit Form */}
              {expandedItemIds.includes(item.id) && editForms[item.id] && (
                <div className={styles.inlineEdit}>
                  <div className={styles.formGroup}>
                    <label>金額</label>
                    <input
                      type="number"
                      value={editForms[item.id].amount}
                      onChange={(e) => setEditForms({
                        ...editForms,
                        [item.id]: { ...editForms[item.id], amount: Number(e.target.value) }
                      })}
                      onWheel={(e) => e.currentTarget.blur()}
                      placeholder="金額"
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label>AIカテゴリ</label>
                    <input
                      type="text"
                      value={editForms[item.id].aiCategory}
                      onChange={(e) => setEditForms({
                        ...editForms,
                        [item.id]: { ...editForms[item.id], aiCategory: e.target.value }
                      })}
                      placeholder="例: 事務用品費"
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label>リスク</label>
                    <select
                      value={editForms[item.id].aiRisk}
                      onChange={(e) => setEditForms({
                        ...editForms,
                        [item.id]: { ...editForms[item.id], aiRisk: e.target.value }
                      })}
                    >
                      <option value="Low">Low</option>
                      <option value="Medium">Medium</option>
                      <option value="High">High</option>
                    </select>
                  </div>

                  <div className={styles.formGroup}>
                    <label>税区分</label>
                    <select
                      value={editForms[item.id].taxType}
                      onChange={(e) => setEditForms({
                        ...editForms,
                        [item.id]: { ...editForms[item.id], taxType: e.target.value }
                      })}
                    >
                      <option value="10%">10%</option>
                      <option value="8%">8%</option>
                      <option value="0%">0%</option>
                    </select>
                  </div>

                  <div className={styles.formGroup}>
                    <label>勘定科目</label>
                    <input
                      type="text"
                      value={editForms[item.id].accountTitle}
                      onChange={(e) => setEditForms({
                        ...editForms,
                        [item.id]: { ...editForms[item.id], accountTitle: e.target.value }
                      })}
                      placeholder="例: 消耗品費"
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label>メモ</label>
                    <textarea
                      value={editForms[item.id].memo}
                      onChange={(e) => setEditForms({
                        ...editForms,
                        [item.id]: { ...editForms[item.id], memo: e.target.value }
                      })}
                      placeholder="メモを入力"
                      rows={2}
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.checkboxLabel}>
                      <input
                        type="checkbox"
                        checked={editForms[item.id].isTaxReturn}
                        onChange={(e) => setEditForms({
                          ...editForms,
                          [item.id]: { ...editForms[item.id], isTaxReturn: e.target.checked }
                        })}
                      />
                      申告対象にする
                    </label>
                  </div>

                  <div className={styles.inlineEditActions}>
                    <button className={styles.saveButton} onClick={() => handleSaveEdit(item.id)}>
                      保存する
                    </button>
                    <button className={styles.cancelButton} onClick={() => handleCancelEdit(item.id)}>
                      キャンセル
                    </button>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      </section>

      {/* Receipt Edit Modal */}
      {isEditingReceipt && (
        <div className={styles.modalOverlay} onClick={() => setIsEditingReceipt(false)}>
          <div className={styles.modalContent} onClick={e => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h2>レシート情報の編集</h2>
              <button className={styles.closeButton} onClick={() => setIsEditingReceipt(false)}>
                <X size={24} />
              </button>
            </div>
            <div className={styles.modalBody}>
              <div className={styles.formGroup}>
                <label>店名</label>
                <input 
                  type="text" 
                  value={receiptEditForm.store}
                  onChange={(e) => setReceiptEditForm({...receiptEditForm, store: e.target.value})}
                />
              </div>
              <div className={styles.formGroup}>
                <label>日付</label>
                <input 
                  type="text" 
                  value={receiptEditForm.date}
                  onChange={(e) => setReceiptEditForm({...receiptEditForm, date: e.target.value})}
                />
              </div>
              <div className={styles.formGroup}>
                <label>電話番号</label>
                <input 
                  type="text" 
                  value={receiptEditForm.tel}
                  onChange={(e) => setReceiptEditForm({...receiptEditForm, tel: e.target.value})}
                />
              </div>
            </div>
            <div className={styles.modalFooter}>
              <button className={styles.saveButton} onClick={handleSaveReceiptEdit}>
                保存する
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
