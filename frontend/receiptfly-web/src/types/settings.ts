// Settings types
export interface AccountTitle {
  id: number;
  name: string;
  isDefault: boolean;
  isFavorite: boolean;
  sortOrder: number;
}

export interface Category {
  id: number;
  name: string;
  isDefault: boolean;
  sortOrder: number;
}

export interface Settings {
  // Appearance
  theme: 'system' | 'light' | 'dark';
  dateFormat: 'yyyy年MM月dd日' | 'yyyy/MM/dd' | 'MM/dd/yyyy';
  fontSize: 'small' | 'medium' | 'large';
  accentColor: string;
  
  // Accounting
  accountTitles: AccountTitle[];
  categories: Category[];
  defaultTaxRate: 10 | 8 | 0;
  taxDisplay: 'inclusive' | 'exclusive';
  defaultTaxReturn: boolean;
  fiscalYear: number;
  businessType: '個人事業主' | '法人' | '副業';
  
  // Data Management
  autoBackup: boolean;
  backupFrequency: 'daily' | 'weekly' | 'monthly';
  dataRetention: boolean;
  retentionPeriod: 1 | 3 | 5 | 7 | 0; // 0 = unlimited
  
  // Receipt Input
  ocrAutoRun: boolean;
  defaultStore: 'empty' | 'previous';
  defaultPaymentMethod: '現金' | 'クレジットカード' | '電子マネー';
  storeAutocomplete: boolean;
  amountComma: boolean;
  itemHistory: boolean;
  
  // Notifications
  reminderEnabled: boolean;
  reminderFrequency: 'daily' | 'weekly' | 'monthly';
  reminderTime: string; // HH:mm format
  highAmountAlert: boolean;
  alertAmount: number;
  riskAlert: boolean;
}

export const DEFAULT_ACCOUNT_TITLES: Omit<AccountTitle, 'id'>[] = [
  { name: '消耗品費', isDefault: true, isFavorite: false, sortOrder: 1 },
  { name: '旅費交通費', isDefault: true, isFavorite: false, sortOrder: 2 },
  { name: '接待交際費', isDefault: true, isFavorite: false, sortOrder: 3 },
  { name: '通信費', isDefault: true, isFavorite: false, sortOrder: 4 },
  { name: '水道光熱費', isDefault: true, isFavorite: false, sortOrder: 5 },
  { name: '地代家賃', isDefault: true, isFavorite: false, sortOrder: 6 },
  { name: '福利厚生費', isDefault: true, isFavorite: false, sortOrder: 7 },
  { name: '会議費', isDefault: true, isFavorite: false, sortOrder: 8 },
  { name: '事業主貸', isDefault: true, isFavorite: false, sortOrder: 9 },
  { name: 'その他', isDefault: true, isFavorite: false, sortOrder: 10 },
];

export const DEFAULT_CATEGORIES: Omit<Category, 'id'>[] = [
  { name: '食費', isDefault: true, sortOrder: 1 },
  { name: '事務用品', isDefault: true, sortOrder: 2 },
  { name: '交通費', isDefault: true, sortOrder: 3 },
  { name: '通信費', isDefault: true, sortOrder: 4 },
  { name: '消耗品', isDefault: true, sortOrder: 5 },
  { name: '会議・打ち合わせ', isDefault: true, sortOrder: 6 },
  { name: '接待・交際', isDefault: true, sortOrder: 7 },
  { name: 'その他', isDefault: true, sortOrder: 8 },
];

export const DEFAULT_SETTINGS: Settings = {
  // Appearance
  theme: 'system',
  dateFormat: 'yyyy年MM月dd日',
  fontSize: 'medium',
  accentColor: '#6366f1',
  
  // Accounting
  accountTitles: DEFAULT_ACCOUNT_TITLES.map((title, index) => ({ ...title, id: index + 1 })),
  categories: DEFAULT_CATEGORIES.map((cat, index) => ({ ...cat, id: index + 1 })),
  defaultTaxRate: 10,
  taxDisplay: 'inclusive',
  defaultTaxReturn: false,
  fiscalYear: new Date().getFullYear(),
  businessType: '個人事業主',
  
  // Data Management
  autoBackup: false,
  backupFrequency: 'weekly',
  dataRetention: false,
  retentionPeriod: 7,
  
  // Receipt Input
  ocrAutoRun: true,
  defaultStore: 'empty',
  defaultPaymentMethod: '現金',
  storeAutocomplete: true,
  amountComma: true,
  itemHistory: true,
  
  // Notifications
  reminderEnabled: false,
  reminderFrequency: 'weekly',
  reminderTime: '20:00',
  highAmountAlert: false,
  alertAmount: 10000,
  riskAlert: true,
};
