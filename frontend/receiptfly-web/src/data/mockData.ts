export interface TransactionItem {
  id: number;
  name: string;
  amount: number;
  isTaxReturn: boolean;
  category?: string;
}

export interface Receipt {
  id: number;
  store: string;
  date: string;
  total: number;
  items: TransactionItem[];
}

export const MOCK_RECEIPTS: Receipt[] = [
  {
    id: 1,
    store: 'スーパーライフ',
    date: '2023年11月22日 10:23',
    total: 1340,
    items: [
      { id: 101, name: 'コピー用紙 A4', amount: 450, isTaxReturn: true, category: '消耗品費' },
      { id: 102, name: '豚肉 300g', amount: 890, isTaxReturn: false, category: '食費' },
    ]
  },
  {
    id: 2,
    store: 'セブンイレブン',
    date: '2023年11月21日 18:45',
    total: 270,
    items: [
      { id: 201, name: 'ボールペン', amount: 120, isTaxReturn: true, category: '消耗品費' },
      { id: 202, name: 'おにぎり', amount: 150, isTaxReturn: false, category: '食費' },
    ]
  },
  {
    id: 3,
    store: 'ユニクロ',
    date: '2023年11月20日 14:30',
    total: 3990,
    items: [
      { id: 301, name: 'ヒートテッククルーネックT', amount: 1290, isTaxReturn: false, category: '被服費' },
      { id: 302, name: 'ヒートテックタイツ', amount: 1290, isTaxReturn: false, category: '被服費' },
      { id: 303, name: '3足組ソックス', amount: 990, isTaxReturn: false, category: '被服費' },
      { id: 304, name: 'ショッピングバッグ', amount: 420, isTaxReturn: true, category: '雑費' }, // Example of tax return item
    ]
  }
];
