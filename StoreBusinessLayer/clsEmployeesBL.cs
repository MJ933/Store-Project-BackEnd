using StoreDataAccessLayer;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace StoreBusinessLayer
{
    public class clsEmployeesBL
    {
        public enum enMode { AddNew = 0, Update = 1 }
        public enMode Mode { get; set; } = enMode.AddNew;

        public EmployeeDTO DTO { get; set; }

        private readonly clsEmployeesDAL _employeesDAL;

        public clsEmployeesBL(EmployeeDTO dto, enMode mode = enMode.AddNew)
        {
            DTO = dto;
            Mode = mode;
            _employeesDAL = new clsEmployeesDAL(clsDataAccessSettingsDAL.CreateDataSource());
        }
        public clsEmployeesBL(clsEmployeesDAL employeesDAL)
        {
            _employeesDAL = employeesDAL;
        }

        public List<EmployeeDTO> GetAllEmployees()
        {
            return _employeesDAL.GetAllEmployees();
        }

        public (List<EmployeeDTO> EmployeesList, int TotalCount) GetEmployeesPaginatedWithFilters(
     int pageNumber, int pageSize, int? employeeID, string? userName, string? email,
     string? phone, string? role, bool? isActive)
        {
            return _employeesDAL.GetEmployeesPaginatedWithFilters(pageNumber, pageSize, employeeID, userName, email, phone, role, isActive);
        }
        public clsEmployeesBL FindEmployeeByEmployeeID(int id)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByEmployeeID(id);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public clsEmployeesBL FindEmployeeByUserName(string userName)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByUserName(userName);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public clsEmployeesBL FindEmployeeByEmail(string email)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByEmail(email);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public clsEmployeesBL FindEmployeeByPhone(string phone)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByPhone(phone);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        private bool _AddNewEmployee()
        {
            int employeeID = _employeesDAL.AddEmployee(this.DTO);
            if (employeeID > 0)
            {
                this.DTO.EmployeeID = employeeID;
                return true;
            }
            return false;
        }

        private bool _UpdateEmployee()
        {
            return _employeesDAL.UpdateEmployee(this.DTO);
        }

        public bool Save()
        {
            switch (this.Mode)
            {
                case enMode.AddNew:
                    if (_AddNewEmployee())
                    {
                        this.Mode = enMode.Update;
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case enMode.Update:
                    return _UpdateEmployee();

                default:
                    return false;
            }
        }

        public bool DeleteEmployeeByEmployeeID(int id)
        {
            return _employeesDAL.DeleteEmployeeByEmployeeID(id);
        }

        public bool IsEmployeeExistsByEmployeeID(int id)
        {
            return _employeesDAL.IsEmployeeExistsByEmployeeID(id);
        }

        public bool IsEmployeeExistsByUserName(string userName)
        {
            return _employeesDAL.IsEmployeeExistsByUserName(userName);
        }

        public clsEmployeesBL GetEmployeeByEmailAndPassword(string email, string password)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByEmailAndPassword(email, password);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public clsEmployeesBL GetEmployeeByPhoneAndPassword(string phone, string password)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByPhoneAndPassword(phone, password);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public clsEmployeesBL GetEmployeeByEmployeeID(int id)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByEmployeeID(id);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }
        public clsEmployeesBL GetEmployeeByUserName(string userName)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByUserName(userName);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }
        public clsEmployeesBL GetEmployeeByEmail(string email)
        {
            EmployeeDTO dto = _employeesDAL.GetEmployeeByEmail(email);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }
        public clsEmployeesBL GetEmployeeByPhone(string phone)
        {
            {
                EmployeeDTO dto = _employeesDAL.GetEmployeeByPhone(phone);

                if (dto != null)
                {
                    return new clsEmployeesBL(dto, enMode.Update);
                }
                else
                {
                    return null;
                }
            }
        }
        public List<EmployeeDTO> GetEmployeesByRole(string role)
        {
            return _employeesDAL.GetEmployeesByRole(role);
        }

        public bool IsEmployeeAdmin(int employeeID)
        {
            return _employeesDAL.IsEmployeeAdmin(employeeID);
        }
    }
}