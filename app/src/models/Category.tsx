interface Category {
  id: number;
  priority: number;
  name: string;
  description: string;
  isDefault?: boolean;
  tags: string[];
}

export default Category;