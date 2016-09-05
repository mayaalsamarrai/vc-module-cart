﻿using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Data.Common;
using VirtoCommerce.CartModule.Data.Model;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.CartModule.Data.Builders
{
	public static class TaxRateProcessor
	{
		public static void ApplyTaxeRates(this ShoppingCart shoppingCart, IEnumerable<TaxRate> taxRates)
		{
			shoppingCart.TaxTotal = 0;

			if (shoppingCart.Items != null)
			{
				foreach (var lineItem in shoppingCart.Items)
				{
					lineItem.ApplyTaxRates(taxRates);
					shoppingCart.TaxTotal += lineItem.TaxTotal;
				}
			}

			if (shoppingCart.Shipments != null)
			{
				foreach (var shipment in shoppingCart.Shipments)
				{
					shipment.ApplyTaxRates(taxRates);
					shoppingCart.TaxTotal += shipment.TaxTotal;
				}
			}
		}

		public static void ApplyTaxRates(this ShippingRate shippingRate, IEnumerable<TaxRate> taxRates)
		{
			var shippingMethodTaxRates = taxRates.Where(x => x.Line.Id.SplitIntoTuple('&').Item1 == shippingRate.ShippingMethod.Code && x.Line.Id.SplitIntoTuple('&').Item2 == shippingRate.OptionName);

			shippingRate.RateWithTax = shippingRate.Rate;

			var shippingMethodTaxRate = shippingMethodTaxRates.FirstOrDefault();
			if (shippingMethodTaxRate != null)
			{
                shippingRate.RateWithTax += shippingMethodTaxRate.Rate;
			}
		}

		public static void ApplyTaxRates(this Shipment shipment, IEnumerable<TaxRate> taxRates)
		{
			shipment.ShippingPriceWithTax = shipment.ShippingPrice;
			
			//Because TaxLine.Id may contains composite string id & extra info
			var shipmentTaxRates = taxRates.Where(x => x.Line.Id.SplitIntoTuple('&').Item1 == shipment.Id).ToList();

			shipment.TaxTotal = 0;

			if (shipmentTaxRates.Any())
			{
				var totalTaxRate = shipmentTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("total"));
				var priceTaxRate = shipmentTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("price"));
				shipment.TaxTotal += totalTaxRate.Rate;
				shipment.ShippingPriceWithTax = shipment.ShippingPrice + priceTaxRate.Rate;
			}
		}

		public static void ApplyTaxRates(this LineItem lineItem, IEnumerable<TaxRate> taxRates)
		{
			lineItem.ListPriceWithTax = lineItem.ListPrice;
			lineItem.SalePriceWithTax = lineItem.SalePrice;
			
			//Because TaxLine.Id may contains composite string id & extra info
			var lineItemTaxRates = taxRates.Where(x => x.Line.Id.SplitIntoTuple('&').Item1 == (lineItem.Id ?? "")).ToList();

			lineItem.TaxTotal = 0;
			
			if (lineItemTaxRates.Any())
			{
				var extendedPriceRate = lineItemTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("extended"));
				var listPriceRate = lineItemTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("list"));
				var salePriceRate = lineItemTaxRates.FirstOrDefault(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("sale"));
				if (salePriceRate == null)
				{
					salePriceRate = listPriceRate;
				}
				lineItem.TaxTotal += extendedPriceRate.Rate * lineItem.Quantity;
				lineItem.ListPriceWithTax = lineItem.ListPrice + listPriceRate.Rate;
				lineItem.SalePriceWithTax = lineItem.SalePrice + salePriceRate.Rate;
			}
		}
	}
}