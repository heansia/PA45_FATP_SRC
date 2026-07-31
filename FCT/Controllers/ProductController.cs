using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models;

namespace FCT.Controllers
{
    /*
    public class ProductController
    {
        // 持有Model和View的引用（构造函数注入，解耦）
        private readonly Product _productModel;
        private readonly frmMain _productView;

        // 构造函数：注入Model和View，初始化控制器
        public ProductController(Product productModel, frmMain productView)
        {
            _productModel = productModel ?? throw new ArgumentNullException(nameof(productModel));
            _productView = productView ?? throw new ArgumentNullException(nameof(productView));
        }

        #region 处理View的事件：加载产品（示例：加载默认产品/从数据库加载）
        public void LoadProduct()
        {
            try
            {
                // 1. 操作Model：模拟从数据库/缓存加载产品数据（Model业务逻辑）
                var defaultProduct = new Product(1, "华为Mate60 Pro", 6999.99m, 1000);
                // 更新Model数据
                _productModel.Id = defaultProduct.Id;
                _productModel.Name = defaultProduct.Name;
                _productModel.Price = defaultProduct.Price;
                _productModel.Stock = defaultProduct.Stock;

                // 2. 向View传递数据：设置界面展示内容
                _productView.ProductId = _productModel.Id.ToString();
                _productView.ProductName = _productModel.Name;
                _productView.ProductPrice = _productModel.Price.ToString("0.00");
                _productView.ProductStock = _productModel.Stock.ToString();

                // 3. 触发View提示：加载成功
                _productView.ShowMessage("产品加载成功！");
            }
            catch (Exception ex)
            {
                _productView.ShowMessage($"加载失败：{ex.Message}");
            }
        }
        #endregion

        #region 处理View的事件：保存产品（核心业务流程）
        public void SaveProduct()
        {
            try
            {
                // 1. 从View读取用户输入数据
                var id = int.Parse(_productView.ProductId);
                var name = _productView.ProductName;
                var price = decimal.Parse(_productView.ProductPrice);
                var stock = int.Parse(_productView.ProductStock);

                // 2. 操作Model执行业务逻辑（数据校验、属性更新）
                _productModel.Id = id;
                _productModel.Name = name;
                _productModel.Price = price;
                _productModel.Stock = stock;

                // 模拟：Model业务逻辑校验（如库存不能超过10000）
                if (_productModel.Stock > 10000)
                {
                    _productView.ShowMessage("保存失败：库存不能超过10000！");
                    return;
                }

                // 3. 模拟：持久化数据（如保存到数据库/文件）
                SaveToDatabase(_productModel);

                // 4. 触发View更新：提示成功+清空输入
                _productView.ShowMessage("产品保存成功！");
                _productView.ClearInput();
            }
            catch (FormatException)
            {
                _productView.ShowMessage("保存失败：请输入正确的格式（ID/库存为整数，价格为数字）！");
            }
            catch (Exception ex)
            {
                _productView.ShowMessage($"保存失败：{ex.Message}");
            }
        }
        #endregion

        #region 私有辅助方法：模拟持久化操作（实际项目替换为EF/ADO.NET等）
        private void SaveToDatabase(Product product)
        {
            // 此处为模拟：实际项目中写数据库插入/更新逻辑
            Console.WriteLine($"【数据库持久化】产品ID：{product.Id}，名称：{product.Name}，价格：{product.Price}，库存：{product.Stock}");
        }
        #endregion

        // 扩展方法：处理库存扣减（演示Model业务逻辑调用）
        public void ReduceProductStock(int buyCount)
        {
            if (_productModel.ReduceStock(buyCount))
            {
                // 库存扣减成功，更新View展示
                _productView.ProductStock = _productModel.Stock.ToString();
                _productView.ShowMessage($"库存扣减成功！剩余库存：{_productModel.Stock}");
                // 持久化更新后的库存
                SaveToDatabase(_productModel);
            }
            else
            {
                _productView.ShowMessage("库存扣减失败：库存不足或购买数量无效！");
            }
        }
    }
    */
}
