import { ArrowLeft, Calendar, Store, Edit2, X } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import styles from './ReceiptDetail.module.css';
import { useReceipts } from '../../context/ReceiptContext';
import { useState } from 'react';
import { type TransactionItem } from '../../data/mockData';

export function ReceiptDetail() {
  const navigate = useNavigate();
  const { id } = useParams();
  const { receipts, updateItem, updateReceipt } = useReceipts();
  const [editingItem, setEditingItem] = useState<TransactionItem | null>(null);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editForm, setEditForm] = useState({ aiCategory: '', aiRisk: '', memo: '', taxType: '', accountTitle: '', isTaxReturn: false });
  const [isEditingReceipt, setIsEditingReceipt] = useState(false);
  const [receiptEditForm, setReceiptEditForm] = useState({ store: '', date: '', tel: '' });
  
  const receipt = receipts.find(r => r.id === Number(id));

  if (!receipt) {
    return <div className={styles.container}>Receipt not found</div>;
  }

  const handleCloseModal = () => {
    setShowEditModal(false);
    setEditingItem(null);
  };

  const toggleTaxReturn = (itemId: number, currentStatus: boolean) => {
    updateItem(receipt.id, itemId, { isTaxReturn: !currentStatus });
  };

  const openEditModal = (item: TransactionItem) => {
    setEditingItem(item);
    setShowEditModal(true);
    setEditForm({
      aiCategory: item.aiCategory || '',
      aiRisk: item.aiRisk || 'Low',
      memo: item.memo || '',
      taxType: item.taxType || '10%',
      accountTitle: item.accountTitle || '',
      isTaxReturn: item.isTaxReturn
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

  const handleSaveEdit = () => {
    if (editingItem) {
      updateItem(receipt.id, editingItem.id, {
        aiCategory: editForm.aiCategory,
        aiRisk: editForm.aiRisk,
        memo: editForm.memo,
        taxType: editForm.taxType,
        accountTitle: editForm.accountTitle,
        isTaxReturn: editForm.isTaxReturn
      });
      setEditingItem(null);
    }
  };

  const taxReturnTotal = receipt.items
    .filter(item => item.isTaxReturn)
    .reduce((sum, item) => sum + item.amount, 0);

  return (
    <div className={`${styles.container} animate-slide-in`}>
      <header className={styles.header}>
        <div className={styles.headerTop}>
          <button onClick={() => navigate(-1)} className={styles.backButton}>
            <ArrowLeft size={24} />
          </button>
          <div className={styles.storeInfo}>
            <Store size={20} />
            <h1>{receipt.store}</h1>
          </div>
          <button onClick={handleReceiptEditClick} className={styles.editReceiptButton}>
            <Edit2 size={18} />
          </button>
        </div>
        <div className={styles.receiptMeta}>
          <div className={styles.metaRow}>
            <Calendar size={16} />
            <span>{receipt.date}</span>
          </div>
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
            <div key={item.id} className={styles.itemRow}> {/* Removed onClick for toggleTaxReturn */}
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
                     openEditModal(item);
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
          ))}
        </div>
      </section>

      {showEditModal && editingItem && (
        <div className={styles.editModal} onClick={handleCloseModal}>
          <div className={styles.editModalContent} onClick={(e) => e.stopPropagation()}>
            <div className={styles.editModalHeader}>
              <h3 className={styles.editModalTitle}>品目編集</h3>
              <button className={styles.editModalClose} onClick={handleCloseModal}>
                <X size={24} />
              </button>
            </div>
            
            <div className={styles.editModalBody}>
              {/* Removed 'category' form group as it's not in TransactionItem */}

              <div className={styles.formGroup}>
                <label>AIカテゴリ</label>
                <input
                  type="text"
                  value={editingItem.aiCategory || ''}
                  onChange={(e) => setEditingItem({ ...editingItem, aiCategory: e.target.value })}
                  placeholder="例: 事務用品費"
                />
              </div>

              <div className={styles.formGroup}>
                <label>リスク</label>
                <select
                  value={editingItem.aiRisk || 'Low'}
                  onChange={(e) => setEditingItem({ ...editingItem, aiRisk: e.target.value })}
                >
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                </select>
              </div>

              <div className={styles.formGroup}>
                <label>税区分</label>
                <select
                  value={editingItem.taxType || '10%'}
                  onChange={(e) => setEditingItem({ ...editingItem, taxType: e.target.value })}
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
                  value={editingItem.accountTitle || ''}
                  onChange={(e) => setEditingItem({ ...editingItem, accountTitle: e.target.value })}
                  placeholder="例: 消耗品費"
                />
              </div>

              <div className={styles.formGroup}>
                <label>メモ</label>
                <textarea
                  value={editingItem.memo || ''}
                  onChange={(e) => setEditingItem({ ...editingItem, memo: e.target.value })}
                  placeholder="メモを入力"
                  rows={3}
                />
              </div>

              <div className={styles.checkboxGroup}>
                <label>
                  <input
                    type="checkbox"
                    checked={editingItem.isTaxReturn}
                    onChange={(e) => setEditingItem({ ...editingItem, isTaxReturn: e.target.checked })}
                  />
                  申告対象にする
                </label>
              </div>

              <button className={styles.saveButton} onClick={handleSaveEdit}>
                保存する
              </button>
            </div>
          </div>
        </div>
      )}

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
