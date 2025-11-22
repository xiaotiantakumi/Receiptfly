import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Sun, Moon, Monitor, Trash2, Download, Plus, X } from 'lucide-react';
import { useSettings } from '../../context/SettingsContext';
import type { AccountTitle, Category } from '../../types/settings';
import styles from './Settings.module.css';

export function Settings() {
  const navigate = useNavigate();
  const { settings, updateSettings, resetSettings } = useSettings();
  
  const [showResetConfirm, setShowResetConfirm] = useState(false);
  const [newAccountTitle, setNewAccountTitle] = useState('');
  const [newCategory, setNewCategory] = useState('');

  const handleThemeChange = (theme: 'system' | 'light' | 'dark') => {
    updateSettings({ theme });
  };

  const handleAddAccountTitle = () => {
    if (!newAccountTitle.trim()) return;
    
    const newTitle: AccountTitle = {
      id: Math.max(...settings.accountTitles.map(t => t.id), 0) + 1,
      name: newAccountTitle.trim(),
      isDefault: false,
      isFavorite: false,
      sortOrder: settings.accountTitles.length + 1,
    };
    
    updateSettings({
      accountTitles: [...settings.accountTitles, newTitle]
    });
    setNewAccountTitle('');
  };

  const handleDeleteAccountTitle = (id: number) => {
    updateSettings({
      accountTitles: settings.accountTitles.filter(t => t.id !== id)
    });
  };

  const handleAddCategory = () => {
    if (!newCategory.trim()) return;
    
    const newCat: Category = {
      id: Math.max(...settings.categories.map(c => c.id), 0) + 1,
      name: newCategory.trim(),
      isDefault: false,
      sortOrder: settings.categories.length + 1,
    };
    
    updateSettings({
      categories: [...settings.categories, newCat]
    });
    setNewCategory('');
  };

  const handleDeleteCategory = (id: number) => {
    updateSettings({
      categories: settings.categories.filter(c => c.id !== id)
    });
  };

  const handleExportCSV = () => {
    // TODO: Implement CSV export
    alert('CSV エクスポート機能は実装中です');
  };

  const handleReset = () => {
    resetSettings();
    setShowResetConfirm(false);
  };

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <button onClick={() => navigate(-1)} className={styles.backButton}>
          <ArrowLeft size={24} />
        </button>
        <h1>設定</h1>
      </header>

      {/* Appearance Section */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>外観</h2>
        
        <div className={styles.settingGroup}>
          <label className={styles.label}>テーマ</label>
          <div className={styles.themeButtons}>
            <button
              className={`${styles.themeButton} ${settings.theme === 'light' ? styles.active : ''}`}
              onClick={() => handleThemeChange('light')}
            >
              <Sun size={20} />
              <span>ライト</span>
            </button>
            <button
              className={`${styles.themeButton} ${settings.theme === 'dark' ? styles.active : ''}`}
              onClick={() => handleThemeChange('dark')}
            >
              <Moon size={20} />
              <span>ダーク</span>
            </button>
            <button
              className={`${styles.themeButton} ${settings.theme === 'system' ? styles.active : ''}`}
              onClick={() => handleThemeChange('system')}
            >
              <Monitor size={20} />
              <span>システム</span>
            </button>
          </div>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.label}>フォントサイズ</label>
          <select
            value={settings.fontSize}
            onChange={(e) => updateSettings({ fontSize: e.target.value as any })}
            className={styles.select}
          >
            <option value="small">小</option>
            <option value="medium">中</option>
            <option value="large">大</option>
          </select>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.label}>日付形式</label>
          <select
            value={settings.dateFormat}
            onChange={(e) => updateSettings({ dateFormat: e.target.value as any })}
            className={styles.select}
          >
            <option value="yyyy年MM月dd日">2025年11月22日</option>
            <option value="yyyy/MM/dd">2025/11/22</option>
            <option value="MM/dd/yyyy">11/22/2025</option>
          </select>
        </div>
      </section>

      {/* Accounting Section */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>会計設定</h2>
        
        <div className={styles.settingGroup}>
          <label className={styles.label}>勘定科目</label>
          <div className={styles.listContainer}>
            {settings.accountTitles.map((title) => (
              <div key={title.id} className={styles.listItem}>
                <span>{title.name}</span>
                {!title.isDefault && (
                  <button
                    onClick={() => handleDeleteAccountTitle(title.id)}
                    className={styles.deleteButton}
                  >
                    <X size={16} />
                  </button>
                )}
              </div>
            ))}
            <div className={styles.addItem}>
              <input
                type="text"
                value={newAccountTitle}
                onChange={(e) => setNewAccountTitle(e.target.value)}
                placeholder="新しい勘定科目"
                className={styles.input}
                onKeyPress={(e) => e.key === 'Enter' && handleAddAccountTitle()}
              />
              <button onClick={handleAddAccountTitle} className={styles.addButton}>
                <Plus size={20} />
              </button>
            </div>
          </div>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.label}>カテゴリ</label>
          <div className={styles.listContainer}>
            {settings.categories.map((category) => (
              <div key={category.id} className={styles.listItem}>
                <span>{category.name}</span>
                {!category.isDefault && (
                  <button
                    onClick={() => handleDeleteCategory(category.id)}
                    className={styles.deleteButton}
                  >
                    <X size={16} />
                  </button>
                )}
              </div>
            ))}
            <div className={styles.addItem}>
              <input
                type="text"
                value={newCategory}
                onChange={(e) => setNewCategory(e.target.value)}
                placeholder="新しいカテゴリ"
                className={styles.input}
                onKeyPress={(e) => e.key === 'Enter' && handleAddCategory()}
              />
              <button onClick={handleAddCategory} className={styles.addButton}>
                <Plus size={20} />
              </button>
            </div>
          </div>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.label}>デフォルト税率</label>
          <select
            value={settings.defaultTaxRate}
            onChange={(e) => updateSettings({ defaultTaxRate: Number(e.target.value) as any })}
            className={styles.select}
          >
            <option value={10}>10%</option>
            <option value={8}>8%（軽減税率）</option>
            <option value={0}>0%（非課税）</option>
          </select>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.toggleLabel}>
            <input
              type="checkbox"
              checked={settings.defaultTaxReturn}
              onChange={(e) => updateSettings({ defaultTaxReturn: e.target.checked })}
            />
            <span>デフォルトで申告対象にする</span>
          </label>
        </div>
      </section>

      {/* Data Management Section */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>データ管理</h2>
        
        <div className={styles.settingGroup}>
          <label className={styles.label}>データエクスポート</label>
          <button onClick={handleExportCSV} className={styles.exportButton}>
            <Download size={20} />
            <span>CSV形式でエクスポート</span>
          </button>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.toggleLabel}>
            <input
              type="checkbox"
              checked={settings.autoBackup}
              onChange={(e) => updateSettings({ autoBackup: e.target.checked })}
            />
            <span>自動バックアップ</span>
          </label>
        </div>

        {settings.autoBackup && (
          <div className={styles.settingGroup}>
            <label className={styles.label}>バックアップ頻度</label>
            <select
              value={settings.backupFrequency}
              onChange={(e) => updateSettings({ backupFrequency: e.target.value as any })}
              className={styles.select}
            >
              <option value="daily">毎日</option>
              <option value="weekly">毎週</option>
              <option value="monthly">毎月</option>
            </select>
          </div>
        )}
      </section>

      {/* Receipt Input Section */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>レシート入力</h2>
        
        <div className={styles.settingGroup}>
          <label className={styles.toggleLabel}>
            <input
              type="checkbox"
              checked={settings.ocrAutoRun}
              onChange={(e) => updateSettings({ ocrAutoRun: e.target.checked })}
            />
            <span>OCR自動実行</span>
          </label>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.label}>デフォルト支払方法</label>
          <select
            value={settings.defaultPaymentMethod}
            onChange={(e) => updateSettings({ defaultPaymentMethod: e.target.value as any })}
            className={styles.select}
          >
            <option value="現金">現金</option>
            <option value="クレジットカード">クレジットカード</option>
            <option value="電子マネー">電子マネー</option>
          </select>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.toggleLabel}>
            <input
              type="checkbox"
              checked={settings.storeAutocomplete}
              onChange={(e) => updateSettings({ storeAutocomplete: e.target.checked })}
            />
            <span>店名の自動補完</span>
          </label>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.toggleLabel}>
            <input
              type="checkbox"
              checked={settings.amountComma}
              onChange={(e) => updateSettings({ amountComma: e.target.checked })}
            />
            <span>金額入力時の自動カンマ挿入</span>
          </label>
        </div>
      </section>

      {/* Notifications Section */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>通知</h2>
        
        <div className={styles.settingGroup}>
          <label className={styles.toggleLabel}>
            <input
              type="checkbox"
              checked={settings.reminderEnabled}
              onChange={(e) => updateSettings({ reminderEnabled: e.target.checked })}
            />
            <span>レシート登録リマインダー</span>
          </label>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.toggleLabel}>
            <input
              type="checkbox"
              checked={settings.highAmountAlert}
              onChange={(e) => updateSettings({ highAmountAlert: e.target.checked })}
            />
            <span>高額レシートアラート</span>
          </label>
        </div>

        {settings.highAmountAlert && (
          <div className={styles.settingGroup}>
            <label className={styles.label}>アラート金額（円）</label>
            <input
              type="number"
              value={settings.alertAmount}
              onChange={(e) => updateSettings({ alertAmount: Number(e.target.value) })}
              className={styles.input}
            />
          </div>
        )}
      </section>

      {/* Danger Zone */}
      <section className={`${styles.section} ${styles.dangerZone}`}>
        <h2 className={styles.sectionTitle}>危険な操作</h2>
        
        <button onClick={() => setShowResetConfirm(true)} className={styles.resetButton}>
          <Trash2 size={20} />
          <span>設定をリセット</span>
        </button>
      </section>

      {/* Reset Confirmation Modal */}
      {showResetConfirm && (
        <div className={styles.modal} onClick={() => setShowResetConfirm(false)}>
          <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
            <h3>設定をリセットしますか？</h3>
            <p>全ての設定がデフォルト値に戻ります。この操作は取り消せません。</p>
            <div className={styles.modalButtons}>
              <button onClick={() => setShowResetConfirm(false)} className={styles.cancelButton}>
                キャンセル
              </button>
              <button onClick={handleReset} className={styles.confirmButton}>
                リセット
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
