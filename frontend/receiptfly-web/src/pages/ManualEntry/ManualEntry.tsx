import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Plus, X } from 'lucide-react';
import { useReceipts } from '../../context/ReceiptContext';
import styles from './ManualEntry.module.css';

interface ItemForm {
  id: string;
  name: string;
  amount: string;
  category: string;
  isTaxReturn: boolean;
}

export function ManualEntry() {
  const navigate = useNavigate();
  const { createReceipt } = useReceipts();
  
  const [store, setStore] = useState('');
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
  const [tel, setTel] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [items, setItems] = useState<ItemForm[]>([
    { id: '1', name: '', amount: '', category: '', isTaxReturn: false }
  ]);
  const [showSuccess, setShowSuccess] = useState(false);
  const [createdReceiptId, setCreatedReceiptId] = useState<number | null>(null);

  const addItem = () => {
    setItems([...items, { 
      id: Date.now().toString(), 
      name: '', 
      amount: '', 
      category: '', 
      isTaxReturn: false 
    }]);
  };

  const removeItem = (id: string) => {
    if (items.length > 1) {
      setItems(items.filter(item => item.id !== id));
    }
  };

  const updateItem = (id: string, field: keyof ItemForm, value: string | boolean) => {
    setItems(items.map(item => 
      item.id === id ? { ...item, [field]: value } : item
    ));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!store || items.some(item => !item.name || !item.amount)) {
      alert('店名と全ての品目情報を入力してください');
      return;
    }

    const receipt = {
      store,
      date: date || new Date().toLocaleDateString('ja-JP'),
      tel,
      paymentMethod,
      items: items.map(item => ({
        name: item.name,
        amount: parseInt(item.amount),
        category: item.category || '未分類',
        isTaxReturn: item.isTaxReturn
      }))
    };

    try {
      const newReceipt = await createReceipt(receipt);
      if (newReceipt) {
        setCreatedReceiptId(newReceipt.id);
        setShowSuccess(true);
      } else {
        alert('レシートの登録に失敗しました');
      }
    } catch (error) {
      console.error('Error creating receipt:', error);
      alert('レシートの登録中にエラーが発生しました');
    }
  };

  const handleContinue = () => {
    // Reset form
    setStore('');
    setDate(new Date().toISOString().split('T')[0]);
    setTel('');
    setPaymentMethod('');
    setItems([{ id: Date.now().toString(), name: '', amount: '', category: '', isTaxReturn: false }]);
    setShowSuccess(false);
    setCreatedReceiptId(null);
  };

  const handleGoHome = () => {
    navigate('/');
  };

  const total = items.reduce((sum, item) => sum + (parseInt(item.amount) || 0), 0);

  return (
    <div className={`${styles.container} animate-fade-in`}>
      <header className={styles.header}>
        <button onClick={() => navigate(-1)} className={styles.backButton}>
          <ArrowLeft size={24} />
        </button>
        <h1>手動入力</h1>
        <div className={styles.placeholder} />
      </header>

      <form onSubmit={handleSubmit} className={styles.form}>
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>レシート情報</h2>
          
          <div className={styles.formGroup}>
            <label>店名 *</label>
            <input 
              type="text" 
              value={store}
              onChange={(e) => setStore(e.target.value)}
              placeholder="例: スーパーライフ"
              required
            />
          </div>

          <div className={styles.formGroup}>
            <label>日付</label>
            <input 
              type="date" 
              value={date}
              onChange={(e) => setDate(e.target.value)}
            />
          </div>

          <div className={styles.formGroup}>
            <label>電話番号</label>
            <input 
              type="tel" 
              value={tel}
              onChange={(e) => setTel(e.target.value)}
              placeholder="例: 03-1234-5678"
            />
          </div>

          <div className={styles.formGroup}>
            <label>支払方法</label>
            <select 
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
            >
              <option value="">選択してください</option>
              <option value="現金">現金</option>
              <option value="クレジットカード">クレジットカード</option>
              <option value="PayPay">PayPay</option>
              <option value="交通系IC">交通系IC</option>
              <option value="その他">その他</option>
            </select>
          </div>
        </section>

        <section className={styles.section}>
          <div className={styles.sectionHeader}>
            <h2 className={styles.sectionTitle}>購入品目</h2>
            <button 
              type="button" 
              onClick={addItem} 
              className={styles.addButton}
            >
              <Plus size={20} />
              品目追加
            </button>
          </div>

          <div className={styles.itemsList}>
            {items.map((item, index) => (
              <div key={item.id} className={styles.itemRow}>
                <div className={styles.itemNumber}>{index + 1}</div>
                
                <div className={styles.itemFields}>
                  <input 
                    type="text" 
                    value={item.name}
                    onChange={(e) => updateItem(item.id, 'name', e.target.value)}
                    placeholder="品目名 *"
                    className={styles.itemName}
                    required
                  />
                  
                  <input 
                    type="number" 
                    value={item.amount}
                    onChange={(e) => updateItem(item.id, 'amount', e.target.value)}
                    placeholder="金額 *"
                    className={styles.itemAmount}
                    required
                  />
                  
                  <input 
                    type="text" 
                    value={item.category}
                    onChange={(e) => updateItem(item.id, 'category', e.target.value)}
                    placeholder="カテゴリ"
                    className={styles.itemCategory}
                  />

                  <label className={styles.checkboxLabel}>
                    <input 
                      type="checkbox"
                      checked={item.isTaxReturn}
                      onChange={(e) => updateItem(item.id, 'isTaxReturn', e.target.checked)}
                    />
                    申告
                  </label>
                </div>

                {items.length > 1 && (
                  <button 
                    type="button"
                    onClick={() => removeItem(item.id)}
                    className={styles.removeButton}
                  >
                    <X size={20} />
                  </button>
                )}
              </div>
            ))}
          </div>
        </section>

        <div className={styles.summary}>
          <span className={styles.summaryLabel}>合計金額</span>
          <span className={styles.summaryAmount}>¥{total.toLocaleString()}</span>
        </div>

        <button type="submit" className={styles.submitButton}>
          レシートを登録
        </button>
      </form>

      {showSuccess && (
        <div className={styles.successModal}>
          <div className={styles.successContent}>
            <div className={styles.successIcon}>✓</div>
            <h2>登録完了</h2>
            <p>レシートを登録しました</p>
            <div className={styles.successButtons}>
              <button 
                className={styles.continueButton}
                onClick={handleContinue}
              >
                続けて登録
              </button>
              <button 
                className={styles.homeButton}
                onClick={handleGoHome}
              >
                ホームへ戻る
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
