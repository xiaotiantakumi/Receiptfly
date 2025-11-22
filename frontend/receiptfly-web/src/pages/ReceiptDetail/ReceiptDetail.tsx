import { ArrowLeft, Edit2, Store, Plus, Trash2, X } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import styles from './ReceiptDetail.module.css';
import { useReceipts } from '../../context/ReceiptContext';
import { useState } from 'react';
import { type TransactionItem } from '../../data/mockData';

import { useSettings } from '../../context/SettingsContext';

export function ReceiptDetail() {
  const navigate = useNavigate();
  const { id } = useParams();
  const { receipts, updateItem, updateReceipt } = useReceipts();
  const { settings } = useSettings();

  const majorAccountTitles = settings.accountTitles
    .filter((t) => t.isFavorite)
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((t) => t.name);

  const otherAccountTitles = settings.accountTitles
    .filter((t) => !t.isFavorite)
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((t) => t.name);

  const majorCategories = settings.categories
    .filter((c) => c.isFavorite)
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((c) => c.name);

  const otherCategories = settings.categories
    .filter((c) => !c.isFavorite)
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((c) => c.name);

  // State to hold form data for each expanded item, keyed by item ID
  const [isEditingReceipt, setIsEditingReceipt] = useState(false);
  const [receiptEditForm, setReceiptEditForm] = useState({
    store: '',
    date: '',
    tel: '',
  });
  // New state to track which items are expanded for inline editing (supports multiple)
  const [expandedItemIds, setExpandedItemIds] = useState<string[]>([]);

  // State to hold form data for each expanded item, keyed by item ID
  const [editForms, setEditForms] = useState<
    Record<
      string,
      {
        name: string;
        debits: { accountTitle: string; amount: number }[];
        credits: { accountTitle: string; amount: number }[];
        aiCategory: string;
        aiRisk: string;
        memo: string;
        taxType: string;
        isTaxReturn: boolean;
      }
    >
  >({});

  const receipt = receipts.find((r) => r.id === id);

  if (!receipt) {
    return <div className={styles.container}>Receipt not found</div>;
  }

  const toggleEditItem = (item: TransactionItem) => {
    setExpandedItemIds((prev) => {
      if (prev.includes(item.id)) {
        // Close if already open
        const newIds = prev.filter((id) => id !== item.id);
        // Clean up form state
        setEditForms((forms) => {
          const newForms = { ...forms };
          delete newForms[item.id];
          return newForms;
        });
        return newIds;
      } else {
        // Open if closed
        // Initialize form state for this item
        setEditForms((forms) => ({
          ...forms,
          [item.id]: {
            name: item.name,
            debits: [
              { accountTitle: item.accountTitle || '', amount: item.amount },
            ],
            credits: [{ accountTitle: '現金', amount: item.amount }], // Default to Cash
            aiCategory: item.aiCategory || '',
            aiRisk: item.aiRisk || 'Low',
            memo: item.memo || '',
            taxType: item.taxType || '10%',
            isTaxReturn: item.isTaxReturn,
          },
        }));
        return [...prev, item.id];
      }
    });
  };

  const handleCancelEdit = (itemId: string) => {
    setExpandedItemIds((prev) => prev.filter((id) => id !== itemId));
    setEditForms((forms) => {
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
      tel: receipt.tel || '',
    });
    setIsEditingReceipt(true);
  };

  const handleSaveReceiptEdit = () => {
    if (receipt) {
      updateReceipt(receipt.id, {
        store: receiptEditForm.store,
        date: receiptEditForm.date,
        tel: receiptEditForm.tel,
      });
      setIsEditingReceipt(false);
    }
  };

  const handleSaveEdit = (itemId: string) => {
    const form = editForms[itemId];
    if (form) {
      const debitTotal = form.debits.reduce((sum, d) => sum + d.amount, 0);
      const creditTotal = form.credits.reduce((sum, c) => sum + c.amount, 0);

      if (debitTotal !== creditTotal) {
        alert(
          `借方合計(${debitTotal})と貸方合計(${creditTotal})が一致していません。`
        );
        return;
      }

      updateItem(receipt.id, itemId, {
        name: form.name,
        amount: debitTotal,
        accountTitle: form.debits[0]?.accountTitle || '', // Use first debit as main title
        aiCategory: form.aiCategory,
        aiRisk: form.aiRisk,
        memo: form.memo,
        taxType: form.taxType,
        isTaxReturn: form.isTaxReturn,
      });
      handleCancelEdit(itemId); // Close and cleanup
    }
  };

  const addDebitRow = (itemId: string) => {
    setEditForms((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        debits: [...prev[itemId].debits, { accountTitle: '', amount: 0 }],
      },
    }));
  };

  const removeDebitRow = (itemId: string, index: number) => {
    setEditForms((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        debits: prev[itemId].debits.filter((_, i) => i !== index),
      },
    }));
  };

  const updateDebitRow = (
    itemId: string,
    index: number,
    field: 'accountTitle' | 'amount',
    value: string | number
  ) => {
    setEditForms((prev) => {
      const newDebits = [...prev[itemId].debits];
      newDebits[index] = { ...newDebits[index], [field]: value };
      return {
        ...prev,
        [itemId]: { ...prev[itemId], debits: newDebits },
      };
    });
  };

  const addCreditRow = (itemId: string) => {
    setEditForms((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        credits: [...prev[itemId].credits, { accountTitle: '', amount: 0 }],
      },
    }));
  };

  const removeCreditRow = (itemId: string, index: number) => {
    setEditForms((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        credits: prev[itemId].credits.filter((_, i) => i !== index),
      },
    }));
  };

  const updateCreditRow = (
    itemId: string,
    index: number,
    field: 'accountTitle' | 'amount',
    value: string | number
  ) => {
    setEditForms((prev) => {
      const newCredits = [...prev[itemId].credits];
      newCredits[index] = { ...newCredits[index], [field]: value };
      return {
        ...prev,
        [itemId]: { ...prev[itemId], credits: newCredits },
      };
    });
  };

  const taxReturnTotal = receipt.items
    .filter((item) => item.isTaxReturn)
    .reduce((sum, item) => sum + item.amount, 0);

  const COMMON_CREDIT_TITLES = [
    '現金',
    'クレジットカード',
    '普通預金',
    '事業主借',
  ];

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
          <button
            onClick={handleReceiptEditClick}
            className={styles.editReceiptButton}
          >
            <Edit2 size={18} />
          </button>
        </div>
      </header>

      <div className={styles.receiptCard}>
        {/* Original cardHeader content removed as store and date are now in the main header */}

        <div className={styles.metaInfo}>
          {receipt.address && (
            <div className={styles.metaRow}>📍 {receipt.address}</div>
          )}
          {receipt.tel && (
            <div className={styles.metaRow}>📞 {receipt.tel}</div>
          )}
          {receipt.paymentMethod && (
            <div className={styles.metaRow}>💳 {receipt.paymentMethod}</div>
          )}
          {receipt.registrationNumber && (
            <div className={styles.metaRow}>
              🔢 T番号: {receipt.registrationNumber}
            </div>
          )}
        </div>

        <div className={styles.totalRow}>
          <div className={styles.totalBlock}>
            <span className={styles.totalLabel}>支払総額</span>
            <span className={styles.totalAmount}>
              ¥{receipt.total.toLocaleString()}
            </span>
          </div>
          <div className={styles.totalBlockRight}>
            <span className={styles.totalLabel}>申告対象</span>
            <span className={styles.taxTotalAmount}>
              ¥{taxReturnTotal.toLocaleString()}
            </span>
          </div>
        </div>
      </div>

      <section className={styles.itemsSection}>
        <h2 className={styles.sectionTitle}>購入品目</h2>
        <div className={styles.itemsList}>
          {receipt.items.map((item) => (
            <div key={item.id}>
              <div
                className={styles.itemRow}
                onClick={() => toggleEditItem(item)}
              >
                {/* Checkbox wrapper removed */}

                <div className={styles.itemInfo}>
                  <span className={styles.itemName}>{item.name}</span>
                  <div className={styles.itemMetaTags}>
                    {item.aiCategory && (
                      <span className={styles.aiTag}>🤖 {item.aiCategory}</span>
                    )}
                    {item.aiRisk && (
                      <span
                        className={`${styles.riskTag} ${
                          styles[item.aiRisk.toLowerCase()]
                        }`}
                      >
                        Risk: {item.aiRisk}
                      </span>
                    )}
                    {item.taxType && (
                      <span className={styles.taxTypeTag}>{item.taxType}</span>
                    )}
                    {item.accountTitle && (
                      <span className={styles.accountTitleTag}>
                        {item.accountTitle}
                      </span>
                    )}
                  </div>
                  {item.memo && (
                    <div className={styles.itemMemo}>📝 {item.memo}</div>
                  )}
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
                    {item.isTaxReturn && (
                      <span className={styles.taxBadge}>申告用</span>
                    )}
                  </div>
                </div>
              </div>

              {/* Inline Edit Form */}
              {expandedItemIds.includes(item.id) && editForms[item.id] && (
                <div className={styles.inlineEdit}>
                  {/* Item Name */}
                  <div className={styles.formGroup}>
                    <label>項目名</label>
                    <input
                      type="text"
                      value={editForms[item.id].name}
                      onChange={(e) =>
                        setEditForms({
                          ...editForms,
                          [item.id]: {
                            ...editForms[item.id],
                            name: e.target.value,
                          },
                        })
                      }
                      placeholder="項目名"
                    />
                  </div>

                  {/* Debit Section */}
                  <div className={styles.journalSection}>
                    <div className={styles.journalHeader}>
                      <label>借方 (Debit)</label>
                      <button
                        onClick={() => addDebitRow(item.id)}
                        className={styles.addJournalRowButton}
                      >
                        <Plus size={14} /> 行追加
                      </button>
                    </div>
                    {editForms[item.id].debits.map((debit, index) => (
                      <div key={`debit-${index}`} className={styles.journalRow}>
                        <div className={styles.journalAccount}>
                          <div className={styles.accountTitleSelector}>
                            <div className={styles.quickSelect}>
                              {majorAccountTitles.map((title) => (
                                <button
                                  key={title}
                                  className={`${styles.accountChip} ${
                                    debit.accountTitle === title
                                      ? styles.active
                                      : ''
                                  }`}
                                  onClick={() =>
                                    updateDebitRow(
                                      item.id,
                                      index,
                                      'accountTitle',
                                      title
                                    )
                                  }
                                >
                                  {title}
                                </button>
                              ))}
                            </div>
                            <select
                              value={debit.accountTitle}
                              onChange={(e) =>
                                updateDebitRow(
                                  item.id,
                                  index,
                                  'accountTitle',
                                  e.target.value
                                )
                              }
                              className={styles.journalSelect}
                            >
                              <option value="">勘定科目を選択</option>
                              <optgroup label="よく使う科目">
                                {majorAccountTitles.map((title) => (
                                  <option key={title} value={title}>
                                    {title}
                                  </option>
                                ))}
                              </optgroup>
                              <optgroup label="その他">
                                {otherAccountTitles.map((title) => (
                                  <option key={title} value={title}>
                                    {title}
                                  </option>
                                ))}
                              </optgroup>
                            </select>
                          </div>
                        </div>
                        <div className={styles.journalAmount}>
                          <input
                            type="number"
                            value={debit.amount}
                            onChange={(e) =>
                              updateDebitRow(
                                item.id,
                                index,
                                'amount',
                                Number(e.target.value)
                              )
                            }
                            onWheel={(e) => e.currentTarget.blur()}
                            placeholder="金額"
                          />
                        </div>
                        {editForms[item.id].debits.length > 1 && (
                          <button
                            onClick={() => removeDebitRow(item.id, index)}
                            className={styles.removeJournalRowButton}
                          >
                            <Trash2 size={14} />
                          </button>
                        )}
                      </div>
                    ))}
                  </div>

                  {/* Credit Section */}
                  <div className={styles.journalSection}>
                    <div className={styles.journalHeader}>
                      <label>貸方 (Credit)</label>
                      <button
                        onClick={() => addCreditRow(item.id)}
                        className={styles.addJournalRowButton}
                      >
                        <Plus size={14} /> 行追加
                      </button>
                    </div>
                    {editForms[item.id].credits.map((credit, index) => (
                      <div
                        key={`credit-${index}`}
                        className={styles.journalRow}
                      >
                        <div className={styles.journalAccount}>
                          <div className={styles.accountTitleSelector}>
                            <div className={styles.quickSelect}>
                              {COMMON_CREDIT_TITLES.map((title) => (
                                <button
                                  key={title}
                                  className={`${styles.accountChip} ${
                                    credit.accountTitle === title
                                      ? styles.active
                                      : ''
                                  }`}
                                  onClick={() =>
                                    updateCreditRow(
                                      item.id,
                                      index,
                                      'accountTitle',
                                      title
                                    )
                                  }
                                >
                                  {title}
                                </button>
                              ))}
                            </div>
                            <select
                              value={credit.accountTitle}
                              onChange={(e) =>
                                updateCreditRow(
                                  item.id,
                                  index,
                                  'accountTitle',
                                  e.target.value
                                )
                              }
                              className={styles.journalSelect}
                            >
                              <option value="">勘定科目を選択</option>
                              <optgroup label="資産・負債">
                                {COMMON_CREDIT_TITLES.map((title) => (
                                  <option key={title} value={title}>
                                    {title}
                                  </option>
                                ))}
                              </optgroup>
                              <optgroup label="すべての勘定科目">
                                {settings.accountTitles.map((t) => (
                                  <option key={t.id} value={t.name}>
                                    {t.name}
                                  </option>
                                ))}
                              </optgroup>
                            </select>
                          </div>
                        </div>
                        <div className={styles.journalAmount}>
                          <input
                            type="number"
                            value={credit.amount}
                            onChange={(e) =>
                              updateCreditRow(
                                item.id,
                                index,
                                'amount',
                                Number(e.target.value)
                              )
                            }
                            onWheel={(e) => e.currentTarget.blur()}
                            placeholder="金額"
                          />
                        </div>
                        {editForms[item.id].credits.length > 1 && (
                          <button
                            onClick={() => removeCreditRow(item.id, index)}
                            className={styles.removeJournalRowButton}
                          >
                            <Trash2 size={14} />
                          </button>
                        )}
                      </div>
                    ))}
                  </div>

                  <div className={styles.formGroup}>
                    <label>メモ</label>
                    <textarea
                      value={editForms[item.id].memo}
                      onChange={(e) =>
                        setEditForms({
                          ...editForms,
                          [item.id]: {
                            ...editForms[item.id],
                            memo: e.target.value,
                          },
                        })
                      }
                      placeholder="メモを入力"
                      rows={2}
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label>リスク</label>
                    <select
                      value={editForms[item.id].aiRisk}
                      onChange={(e) =>
                        setEditForms({
                          ...editForms,
                          [item.id]: {
                            ...editForms[item.id],
                            aiRisk: e.target.value,
                          },
                        })
                      }
                    >
                      <option value="Low">Low</option>
                      <option value="Medium">Medium</option>
                      <option value="High">High</option>
                    </select>
                  </div>

                  {/* Category (Moved to bottom) */}
                  <div className={styles.formGroup}>
                    <label>カテゴリ</label>
                    <div className={styles.accountTitleSelector}>
                      <div className={styles.quickSelect}>
                        {majorCategories.map((cat) => (
                          <button
                            key={cat}
                            className={`${styles.accountChip} ${
                              editForms[item.id].aiCategory === cat
                                ? styles.active
                                : ''
                            }`}
                            onClick={() =>
                              setEditForms({
                                ...editForms,
                                [item.id]: {
                                  ...editForms[item.id],
                                  aiCategory: cat,
                                },
                              })
                            }
                          >
                            {cat}
                          </button>
                        ))}
                      </div>
                      <select
                        value={editForms[item.id].aiCategory}
                        onChange={(e) => {
                          setEditForms({
                            ...editForms,
                            [item.id]: {
                              ...editForms[item.id],
                              aiCategory: e.target.value,
                            },
                          });
                        }}
                        className={styles.otherSelect}
                      >
                        <option value="">選択してください</option>
                        <optgroup label="よく使うカテゴリ">
                          {majorCategories.map((cat) => (
                            <option key={cat} value={cat}>
                              {cat}
                            </option>
                          ))}
                        </optgroup>
                        <optgroup label="その他">
                          {otherCategories.map((cat) => (
                            <option key={cat} value={cat}>
                              {cat}
                            </option>
                          ))}
                        </optgroup>
                      </select>
                    </div>
                  </div>

                  <div className={styles.formGroup}>
                    <label>税区分</label>
                    <select
                      value={editForms[item.id].taxType}
                      onChange={(e) =>
                        setEditForms({
                          ...editForms,
                          [item.id]: {
                            ...editForms[item.id],
                            taxType: e.target.value,
                          },
                        })
                      }
                    >
                      <option value="10%">10%</option>
                      <option value="8%">8%</option>
                      <option value="0%">0%</option>
                    </select>
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.checkboxLabel}>
                      <input
                        type="checkbox"
                        checked={editForms[item.id].isTaxReturn}
                        onChange={(e) =>
                          setEditForms({
                            ...editForms,
                            [item.id]: {
                              ...editForms[item.id],
                              isTaxReturn: e.target.checked,
                            },
                          })
                        }
                      />
                      申告対象にする
                    </label>
                  </div>

                  <div className={styles.inlineEditActions}>
                    <button
                      className={styles.cancelButton}
                      onClick={() => handleCancelEdit(item.id)}
                    >
                      キャンセル
                    </button>
                    <button
                      className={styles.saveButton}
                      onClick={() => handleSaveEdit(item.id)}
                      disabled={
                        editForms[item.id].debits.reduce(
                          (sum, d) => sum + d.amount,
                          0
                        ) !==
                        editForms[item.id].credits.reduce(
                          (sum, c) => sum + c.amount,
                          0
                        )
                      }
                    >
                      保存
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
        <div
          className={styles.modalOverlay}
          onClick={() => setIsEditingReceipt(false)}
        >
          <div
            className={styles.modalContent}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.modalHeader}>
              <h2>レシート情報の編集</h2>
              <button
                className={styles.closeButton}
                onClick={() => setIsEditingReceipt(false)}
              >
                <X size={24} />
              </button>
            </div>
            <div className={styles.modalBody}>
              <div className={styles.formGroup}>
                <label>店名</label>
                <input
                  type="text"
                  value={receiptEditForm.store}
                  onChange={(e) =>
                    setReceiptEditForm({
                      ...receiptEditForm,
                      store: e.target.value,
                    })
                  }
                />
              </div>
              <div className={styles.formGroup}>
                <label>日付</label>
                <input
                  type="text"
                  value={receiptEditForm.date}
                  onChange={(e) =>
                    setReceiptEditForm({
                      ...receiptEditForm,
                      date: e.target.value,
                    })
                  }
                />
              </div>
              <div className={styles.formGroup}>
                <label>電話番号</label>
                <input
                  type="text"
                  value={receiptEditForm.tel}
                  onChange={(e) =>
                    setReceiptEditForm({
                      ...receiptEditForm,
                      tel: e.target.value,
                    })
                  }
                />
              </div>
            </div>
            <div className={styles.modalFooter}>
              <button
                className={styles.saveButton}
                onClick={handleSaveReceiptEdit}
              >
                保存する
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
