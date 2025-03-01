using StoreDataAccessLayer;
using System;
using System.Collections.Generic;

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

        public (List<CustomerDTO> CustomersList, int TotalCount) GetCustomersPaginatedWithFilters(
    int pageNumber, int pageSize, int? customerID, string? firstName,
    string? lastName, string? email, string? phone, DateTime? registeredAt, bool? isActive)
        {
            return _customersDAL.GetCustomersPaginatedWithFilters(pageNumber, pageSize, customerID, firstName, lastName, email, phone, registeredAt, isActive);
        }

        public clsCustomersBL GetCustomerByCustomerID(int id)
        {
            CustomerDTO dto = _customersDAL.GetCustomerByCustomerID(id);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public clsCustomersBL GetCustomerByCustomerPhone(string phone)
        {
            CustomerDTO dto = _customersDAL.GetCustomerByCustomerPhone(phone);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public clsCustomersBL GetCustomerByCustomerEmail(string email)
        {
            CustomerDTO dto = _customersDAL.GetCustomerByCustomerEmail(email);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        private bool _AddNewCustomer()
        {
            int customerID = _customersDAL.AddCustomer(this.DTO);
            if (customerID > 0)
            {
                this.DTO.CustomerID = customerID;
                return true;
            }
            return false;
        }

        private bool _UpdateCustomer()
        {
            return _customersDAL.UpdateCustomer(this.DTO);
        }

        public bool Save()
        {
            switch (this.Mode)
            {
                case enMode.AddNew:
                    if (_AddNewCustomer())
                    {
                        this.Mode = enMode.Update;
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case enMode.Update:
                    return _UpdateCustomer();

                default:
                    return false;
            }
        }

        public bool DeleteCustomerByCustomerID(int id)
        {
            return _customersDAL.DeleteCustomerByCustomerID(id);
        }

        public bool IsCustomerExistsByCustomerID(int id)
        {
            return _customersDAL.IsCustomerExistsByCustomerID(id);
        }


        public bool IsCustomerExistsByCustomerPhone(string phone)
        {
            return _customersDAL.IsCustomerExistsByCustomerPhone(phone);
        }

        public bool IsCustomerExistsByCustomerEmail(string email)
        {
            return _customersDAL.IsCustomerExistsByCustomerEmail(email);
        }

        public clsCustomersBL GetCustomerByEmailAndPassword(string email, string password)
        {
            CustomerDTO dto = _customersDAL.GetCustomerByEmailAndPassword(email, password);

            if (dto != null)
            {
                return new clsCustomersBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public clsCustomersBL GetCustomerByPhoneAndPassword(string phone, string password)
        {
            CustomerDTO dto = _customersDAL.GetCustomerByPhoneAndPassword(phone, password);

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