﻿using RestWithAspNet5.Data.Converter.Contract;
using RestWithAspNet5.Data.VO;
using RestWithAspNet5.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestWithAspNet5.Data.Converter.Implementations
{
    public class PersonConverter : IParser<PersonVO, Person>, IParser<Person, PersonVO>
    {
        public Person Parse(PersonVO origin)
        {
            if (origin == null)
            {
                return null;
            }

            return new Person()
            {
                Id = origin.Id,
                FirstName = origin.FirstName,
                LastName = origin.LastName,
                Address = origin.Address,
                Gender = origin.Gender
            };
        }

        public List<Person> Parse(List<PersonVO> origins)
        {
            if (origins == null)
            {
                return null;
            }

            return origins.Select(item => Parse(item)).ToList();
        }

        public PersonVO Parse(Person origin)
        {
            if (origin == null)
            {
                return null;
            }

            return new PersonVO()
            {
                Id = origin.Id,
                FirstName = origin.FirstName,
                LastName = origin.LastName,
                Address = origin.Address,
                Gender = origin.Gender
            };
        }

        public List<PersonVO> Parse(List<Person> origins)
        {
            if (origins == null)
            {
                return null;
            }

            return origins.Select(item => Parse(item)).ToList();
        }
    }
}
