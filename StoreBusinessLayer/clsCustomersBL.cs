using StoreDataAccessLayer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoreBusinessLayer
{
    public class clsCustomersBL
    {
        public enum enMode { AddNew = 0, Update = 1 }
        public enMode Mode { get; set; } = enMode.AddNew;

        public CustomerDTO DTO { get; set; }

        private readonly clsCustomersDAL _customersDAL;

        public clsCustomersBL(CustomerDTO dto, enMode mode = enMode.AddNew)
        {
            this.DTO = dto;
            this.Mode = mode;
            _customersDAL = new clsCustomersDAL(clsDataAccessSettingsDAL.CreateDataSource());
        }

        public clsCustomersBL(clsCustomersDAL customersDAL)
        {
            _customersDAL = customersDAL;
        }

        public async Task<(List<CustomerDTO> CustomersList, int TotalCount)> GetCustomersPaginatedWithFilters(
            int pageNumber, int pageSize, int? customerID, string? firstName,
            string? lastName, string? email, string? phone, DateTime? registeredAt, bool? isActive)
        {
            return await _customersDAL.GetCustomersPaginatedWithFilters(pageNumber, pageSize, customerID, firstName, lastName, email, phone, registeredAt, isActive);
        }

        public async Task<clsCustomersBL> GetCustomerByCustomerID(int id)
        {
            CustomerDTO dto = await _customersDAL.GetCustomerByCustomerID(id);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public async Task<clsCustomersBL> GetCustomerByCustomerPhone(string phone)
        {
            CustomerDTO dto = await _customersDAL.GetCustomerByCustomerPhone(phone);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public async Task<clsCustomersBL> GetCustomerByCustomerEmail(string email)
        {
            CustomerDTO dto = await _customersDAL.GetCustomerByCustomerEmail(email);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        private async Task<bool> _AddNewCustomer()
        {
            int customerID = await _customersDAL.AddCustomer(this.DTO);
            if (customerID > 0)
            {
                this.DTO.CustomerID = customerID;
                return true;
            }
            return false;
        }

        private async Task<bool> _UpdateCustomer()
        {
            return await _customersDAL.UpdateCustomer(this.DTO);
        }

        public async Task<bool> Save()
        {
            switch (this.Mode)
            {
                case enMode.AddNew:
                    if (await _AddNewCustomer())
                    {
                        this.Mode = enMode.Update;
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case enMode.Update:
                    return await _UpdateCustomer();

                default:
                    return false;
            }
        }

        public async Task<bool> DeleteCustomerByCustomerID(int id)
        {
            return await _customersDAL.DeleteCustomerByCustomerID(id);
        }

        public async Task<bool> IsCustomerExistsByCustomerID(int id)
        {
            return await _customersDAL.IsCustomerExistsByCustomerID(id);
        }

        public async Task<bool> IsCustomerExistsByCustomerPhone(string phone)
        {
            return await _customersDAL.IsCustomerExistsByCustomerPhone(phone);
        }

        public async Task<bool> IsCustomerExistsByCustomerEmail(string email)
        {
            return await _customersDAL.IsCustomerExistsByCustomerEmail(email);
        }

        public async Task<clsCustomersBL> GetCustomerByEmailAndPassword(string email, string password)
        {
            CustomerDTO dto = await _customersDAL.GetCustomerByEmailAndPassword(email, password);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public async Task<clsCustomersBL> GetCustomerByPhoneAndPassword(string phone, string password)
        {
            CustomerDTO dto = await _customersDAL.GetCustomerByPhoneAndPassword(phone, password);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }
    }
}