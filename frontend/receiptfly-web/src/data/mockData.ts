export interface TransactionItem {
  id: number;
  name: string;
  amount: number;
  isTaxReturn: boolean;
  category?: string;
  aiCategory?: string;
  aiRisk?: string;
  memo?: string;
  taxType?: string;
  accountTitle?: string;
}

export interface Receipt {
  id: number;
  store: string;
  date: string;
  total: number;
  address?: string;
  tel?: string;
  paymentMethod?: string;
  registrationNumber?: string;
  creditAccount?: string;
  items: TransactionItem[];
}

export const MOCK_RECEIPTS: Receipt[] = [
  {
    id: 1,
    store: 'スーパーライフ',
    date: '2023年11月22日 10:23',
    total: 1340,
    address: '東京都渋谷区1-2-3',
    tel: '03-1234-5678',
    paymentMethod: 'クレジットカード',
    registrationNumber: 'T1234567890123',
    items: [
      { 
        id: 101, 
        name: 'コピー用紙 A4', 
        amount: 450, 
        isTaxReturn: true, 
        category: '消耗品費',
        aiCategory: '消耗品費',
        aiRisk: 'Low',
        memo: 'プリンター用'
      },
      { 
        id: 102, 
        name: '豚肉 300g', 
        amount: 890, 
        isTaxReturn: false, 
        category: '食費',
        aiCategory: '食費',
        aiRisk: 'Low'
      },
    ]
  },
  {
    id: 2,
    store: 'セブンイレブン',
    date: '2023年11月21日 18:45',
    total: 270,
    address: '東京都新宿区西新宿1-1-1',
    tel: '03-9876-5432',
    paymentMethod: 'PayPay',
    registrationNumber: 'T9876543210987',
    items: [
      { 
        id: 201, 
        name: 'ボールペン', 
        amount: 120, 
        isTaxReturn: true, 
        category: '消耗品費',
        aiCategory: '事務用品費',
        aiRisk: 'Low',
        memo: 'クライアント訪問用'
      },
      { 
        id: 202, 
        name: 'おにぎり', 
        amount: 150, 
        isTaxReturn: false, 
        category: '食費',
        aiCategory: '食費',
        aiRisk: 'Low'
      },
    ]
  },
  {
    id: 3,
    store: 'ユニクロ',
    date: '2023年11月20日 14:30',
    total: 3990,
    address: '東京都中央区銀座6-9-5',
    tel: '03-1111-2222',
    paymentMethod: 'クレジットカード',
    registrationNumber: 'T1122334455667',
    items: [
      { 
        id: 301, 
        name: 'ヒートテッククルーネックT', 
        amount: 1290, 
        isTaxReturn: false, 
        category: '被服費',
        aiCategory: '被服費',
        aiRisk: 'Medium',
        memo: '私用？'
      },
      { 
        id: 302, 
        name: 'ヒートテックタイツ', 
        amount: 1290, 
        isTaxReturn: false, 
        category: '被服費',
        aiCategory: '被服費',
        aiRisk: 'Medium'
      },
      { 
        id: 303, 
        name: '3足組ソックス', 
        amount: 990, 
        isTaxReturn: false, 
        category: '被服費',
        aiCategory: '被服費',
        aiRisk: 'Low'
      },
      { 
        id: 304, 
        name: 'ショッピングバッグ', 
        amount: 420, 
        isTaxReturn: true, 
        category: '雑費',
        aiCategory: '消耗品費',
        aiRisk: 'Low',
        memo: '資料持ち運び用'
      },
    ]
  },
  {
    id: 4,
    store: 'タクシー（日本交通）',
    date: '2023年11月19日 23:15',
    total: 4500,
    address: '車内',
    tel: '03-3333-4444',
    paymentMethod: 'GO Pay',
    registrationNumber: 'T5566778899001',
    items: [
      { 
        id: 401, 
        name: '乗車料金', 
        amount: 4500, 
        isTaxReturn: true, 
        category: '旅費交通費',
        aiCategory: '旅費交通費',
        aiRisk: 'Medium',
        memo: '深夜帰宅（プロジェクト遅延対応）'
      }
    ]
  },
  {
    id: 5,
    store: '居酒屋 魚金',
    date: '2023年11月18日 20:00',
    total: 12000,
    address: '東京都港区新橋3-3-3',
    tel: '03-5555-6666',
    paymentMethod: '現金',
    registrationNumber: 'T9988776655443',
    items: [
      { 
        id: 501, 
        name: '飲食代', 
        amount: 12000, 
        isTaxReturn: true, 
        category: '交際費',
        aiCategory: '接待交際費',
        aiRisk: 'High',
        memo: '取引先（株式会社A）との会食'
      }
    ]
  }
];
